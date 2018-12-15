using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Face_Recogniation_using_Azure_Face_API.Properties;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using Newtonsoft.Json.Linq;
using AForge.Video;
using System.Threading;
// This is the code for your desktop app.
// Press Ctrl+F5 (or go to Debug > Start Without Debugging) to run your app.

namespace Face_Recogniation_using_Azure_Face_API
{
    public partial class Form1 : Form
    {
        string Image_Path;
        IFaceServiceClient faceServiceClient;
        string personGroupId = "mygroup";
        LogsDatabase db = new LogsDatabase();
        const string databaseName = "Mydata";
        MJPEGStream stream;// = new MJPEGStream();

        //https://canadacentral.api.cognitive.microsoft.com/
        //e0116f6a2def416cafb0e87f2b3c3760

        public Form1()
        {
            InitializeComponent();
            Image_Path = null;
            try
            {
                db.ReadXml(databaseName);
            }
            catch (Exception ex)
            {
                string user = "Admin";
                string password = "123";
                db.Users.AddUsersRow(user, password);
                db.WriteXml(databaseName);
            }
        }

        

        private void button1_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Thanks!");
        }

        private void btnOpen_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                Image_Path = openFileDialog1.FileName;
                pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
                pictureBox1.Image = new Bitmap(Image_Path);
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            faceServiceClient = new FaceServiceClient(txtKey.Text, txtEndPoint.Text);
            MessageBox.Show("Done !!");
        }

        private void btnGrop_Click(object sender, EventArgs e)
        {
            if (txtKey.Text.Length <= 0 || txtEndPoint.Text.Length <= 0)
            {
                faceServiceClient = new FaceServiceClient("fb2c36c2a96b4014b478833653ddeffa", "https://canadacentral.api.cognitive.microsoft.com/face/v1.0");
            }

            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                string[] subdirectoryEntries = Directory.GetDirectories(folderBrowserDialog1.SelectedPath);
                listBox1.Items.Clear();
                foreach (string subdirectory in subdirectoryEntries)
                {
                    listBox1.Items.Add(subdirectory);
                }
            }
        }

        private async void btnTrain_ClickAsync(object sender, EventArgs e)
        {
           
            bool GroupExist;
            try
            {
                // Create an empty PersonGroup
                await faceServiceClient.GetPersonGroupAsync(personGroupId);
                GroupExist = true;

            }
            catch (Exception ex )
            {
                await faceServiceClient.CreatePersonGroupAsync(personGroupId, "my group");
            }
            lblState.Text = "please wait it's in training phase ....";
            for (int index = 0; index < listBox1.Items.Count; index++)
            {
                string p = new DirectoryInfo(listBox1.Items[index].ToString()).Name;
                await RegisterNameAsync(p, listBox1.Items[index].ToString(), personGroupId);
            }
            await TrainAsync(personGroupId);
        }
        async Task TrainAsync(string personGroupId)
        {
            await faceServiceClient.TrainPersonGroupAsync(personGroupId);
            TrainingStatus trainingStatus = null;
            while (true)
            {
                lblState.Text = "please wait it's in training phase ....";
                trainingStatus = await faceServiceClient.GetPersonGroupTrainingStatusAsync(personGroupId);

                if (trainingStatus.Status != Status.Running)
                {
                    MessageBox.Show("ML trained ...");
                    lblState.Text = "ML trained ...";
                    break;
                }

                await Task.Delay(1000);
            }
        }

        private async Task RegisterNameAsync(string person, string path, string personGroupId)
        {
            
            foreach (string imagePath in Directory.GetFiles(path, "*.jpg"))
            {
                // Define person
                CreatePersonResult friend1 = await faceServiceClient.CreatePersonAsync(
                    // Id of the PersonGroup that the person belonged to
                    personGroupId,
                    // Name of the person
                    person
                );
                
                using (Stream s = File.OpenRead(imagePath))
                {
                    // Detect faces in the image and add to Anna
                    await faceServiceClient.AddPersonFaceAsync(
                        personGroupId, friend1.PersonId, s);
                }
            }
            var em = db.Tables["Employee"].Select("Emp_name like '" + person + "'");
            if (em.Count() <= 0)
            {
                db.Employee.AddEmployeeRow(person);
                db.WriteXml(databaseName);
            }
        }

        private async void btnIdentify_ClickAsync(object sender, EventArgs e)
        {
            richTextBox1.Text = "";
            try
            {
                if (Image_Path != null || Image_Path.Length > 0)
                {
                    using (Stream s = File.OpenRead(Image_Path))
                    {
                        var faces = await faceServiceClient.DetectAsync(s);
                        var faceIds = faces.Select(face => face.FaceId).ToArray();
                        try 
                        {
                            var results = await faceServiceClient.IdentifyAsync(personGroupId, faceIds);
                            foreach (var identifyResult in results)
                            {
                                //Console.WriteLine("Result of face: {0}", identifyResult.FaceId);
                                richTextBox1.AppendText("Result of face: " + identifyResult.FaceId);
                                if (identifyResult.Candidates.Length == 0)
                                {
                                    //Console.WriteLine("No one identified");
                                    richTextBox1.AppendText("\r\nNo one identified");
                                }
                                else
                                {
                                    // Get top 1 among all candidates returned
                                    var candidateId = identifyResult.Candidates[0].PersonId;
                                    var person = await faceServiceClient.GetPersonAsync(personGroupId, candidateId);
                                    //Console.WriteLine("Identified as {0}", person.Name);
                                    richTextBox1.AppendText("\r\nIdentified as " + person.Name);
                                    for (int i = 0; i < db.Employee.Rows.Count; i++)
                                    {
                                        if (db.Employee.Rows[i]["Emp_name"].ToString().Equals(person.Name))
                                        {
                                            DataRow[] foundRows;
                                            foundRows = db.Tables["Logs"].Select("Emp_Name Like '" + person.Name + "' AND Login_date like '" + DateTime.Now.Date.ToString() + "'");
                                            if (foundRows.Count() > 0)
                                            {
                                                db.Logs.AddLogsRow(person.Name, DateTime.Now.Date.ToString(), "");
                                            }
                                            bool x = false;
                                            int j;
                                            for (j = 0; j < db.Logs.Rows.Count; j++)
                                            {
                                                if (db.Logs.Rows[j]["Emp_Name"].ToString().Equals(person.Name) && db.Logs.Rows[j]["Login_date"].ToString().Equals(DateTime.Now.Date.ToString()))
                                                {
                                                    x = true;
                                                    break;
                                                }
                                            }
                                            if (!x)
                                            {
                                                db.Logs.AddLogsRow(person.Name, DateTime.Now.Date.ToString(), "");
                                            }
                                            else
                                            {
                                                db.Logs.Rows[j]["Logout_date"] = DateTime.Now.Date.ToString();
                                            }
                                            db.WriteXml(databaseName);
                                            //
                                        }
                                    }
                                }
                            }
                        }
                        catch(Exception ex)
                        {
                            MessageBox.Show("not found..");
                        }

                    }
                }
            }
            catch(Exception ex)
            {
                if (pictureBox2.Image != null)
                {
                    pictureBox2.Image.Save("temp.jpg");
                    using (Stream s = File.OpenRead("temp.jpg"))
                    {
                        var faces = await faceServiceClient.DetectAsync(s);
                        var faceIds = faces.Select(face => face.FaceId).ToArray();
                        try
                        {
                            var results = await faceServiceClient.IdentifyAsync(personGroupId, faceIds);
                            foreach (var identifyResult in results)
                            {
                                //Console.WriteLine("Result of face: {0}", identifyResult.FaceId);
                                richTextBox1.AppendText("Result of face: " + identifyResult.FaceId);
                                if (identifyResult.Candidates.Length == 0)
                                {
                                    //Console.WriteLine("No one identified");
                                    richTextBox1.AppendText("\r\nNo one identified");
                                }
                                else
                                {
                                    // Get top 1 among all candidates returned
                                    var candidateId = identifyResult.Candidates[0].PersonId;
                                    var person = await faceServiceClient.GetPersonAsync(personGroupId, candidateId);
                                    //Console.WriteLine("Identified as {0}", person.Name);
                                    richTextBox1.AppendText("\r\nIdentified as " + person.Name);
                                    for (int i = 0; i < db.Employee.Rows.Count; i++)
                                    {
                                        if (db.Employee.Rows[i]["Emp_name"].ToString().Equals(person.Name))
                                        {
                                            DataRow[] foundRows;
                                            foundRows = db.Tables["Logs"].Select("Emp_Name Like '" + person.Name + "' AND Login_date like '" + DateTime.Now.Date.ToString() + "'");
                                            if (foundRows.Count() > 0)
                                            {
                                                db.Logs.AddLogsRow(person.Name, DateTime.Now.Date.ToString(), "");
                                            }
                                            bool x = false;
                                            int j;
                                            for (j = 0; j < db.Logs.Rows.Count; j++)
                                            {
                                                if (db.Logs.Rows[j]["Emp_Name"].ToString().Equals(person.Name) && db.Logs.Rows[j]["Login_date"].ToString().Equals(DateTime.Now.Date.ToString()))
                                                {
                                                    x = true;
                                                    break;
                                                }
                                            }
                                            if (!x)
                                            {
                                                db.Logs.AddLogsRow(person.Name, DateTime.Now.Date.ToString(), "");
                                            }
                                            else
                                            {
                                                db.Logs.Rows[j]["Logout_date"] = DateTime.Now.Date.ToString();
                                            }
                                            db.WriteXml(databaseName);
                                            //
                                        }
                                    }
                                }
                            }
                        }
                        catch(Exception ex1)
                        {
                            MessageBox.Show("not found..");
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Error no image to check ...");
                }
            }
        }
       
        static async void MakeAnalysisRequest(string imageFilePath)
        {
            HttpClient client = new HttpClient();
            string uriBase = "https://canadacentral.api.cognitive.microsoft.com/face/v1.0/detect";
            string subscriptionKey = "fb2c36c2a96b4014b478833653ddeffa";

            // Request headers.
            client.DefaultRequestHeaders.Add(
                "Ocp-Apim-Subscription-Key", subscriptionKey);

            // Request parameters. A third optional parameter is "details".
            string requestParameters = "returnFaceId=true&returnFaceLandmarks=false" +
                "&returnFaceAttributes=age,gender,headPose,smile,facialHair,glasses," +
                "emotion,hair,makeup,occlusion,accessories,blur,exposure,noise";

            // Assemble the URI for the REST API Call.
            string uri = uriBase + "?" + requestParameters;

            HttpResponseMessage response;

            // Request body. Posts a locally stored JPEG image.
            byte[] byteData = GetImageAsByteArray(imageFilePath);

            using (ByteArrayContent content = new ByteArrayContent(byteData))
            {
                // This example uses content type "application/octet-stream".
                // The other content types you can use are "application/json"
                // and "multipart/form-data".
                content.Headers.ContentType =
                    new MediaTypeHeaderValue("application/octet-stream");

                // Execute the REST API call.
                response = await client.PostAsync(uri, content);

                // Get the JSON response.
                string contentString = await response.Content.ReadAsStringAsync();
                var obj = JArray.Parse(JsonPrettyPrint(contentString));

                foreach (JObject o in obj.Children<JObject>())
                {
                    foreach (JProperty p in o.Properties())
                    {
                        string name = p.Name;
                        try
                        {
                            string value = (string)p.Value;
                            Console.WriteLine(name + " -- " + value);
                        }
                        catch (Exception ex)
                        {
                            foreach (JObject item in p.Children<JObject>())
                            {
                                Console.WriteLine(item);
                            }
                        }
                       
                    }
                }
                //var emotion = obj["emotion"];
                // Display the JSON response.
                //Console.WriteLine("\nResponse:\n");
                //Console.WriteLine(JsonPrettyPrint(contentString));
                //Console.WriteLine("\nPress Enter to exit...");
            }
        }

        // Formats the given JSON string by adding line breaks and indents.
        static string JsonPrettyPrint(string json)
        {
            if (string.IsNullOrEmpty(json))
                return string.Empty;

            json = json.Replace(Environment.NewLine, "").Replace("\t", "");

            StringBuilder sb = new StringBuilder();
            bool quote = false;
            bool ignore = false;
            int offset = 0;
            int indentLength = 3;

            foreach (char ch in json)
            {
                switch (ch)
                {
                    case '"':
                        if (!ignore) quote = !quote;
                        break;
                    case '\'':
                        if (quote) ignore = !ignore;
                        break;
                }

                if (quote)
                    sb.Append(ch);
                else
                {
                    switch (ch)
                    {
                        case '{':
                        case '[':
                            sb.Append(ch);
                            sb.Append(Environment.NewLine);
                            sb.Append(new string(' ', ++offset * indentLength));
                            break;
                        case '}':
                        case ']':
                            sb.Append(Environment.NewLine);
                            sb.Append(new string(' ', --offset * indentLength));
                            sb.Append(ch);
                            break;
                        case ',':
                            sb.Append(ch);
                            sb.Append(Environment.NewLine);
                            sb.Append(new string(' ', offset * indentLength));
                            break;
                        case ':':
                            sb.Append(ch);
                            sb.Append(' ');
                            break;
                        default:
                            if (ch != ' ') sb.Append(ch);
                            break;
                    }
                }
            }

            return sb.ToString().Trim();
        }

        // Returns the contents of the specified file as a byte array.
        static byte[] GetImageAsByteArray(string imageFilePath)
        {
            using (FileStream fileStream =
                new FileStream(imageFilePath, FileMode.Open, FileAccess.Read))
            {
                BinaryReader binaryReader = new BinaryReader(fileStream);
                return binaryReader.ReadBytes((int)fileStream.Length);
            }
        }

        private void btnStream_Click(object sender, EventArgs e)
        {
            if (txtIp.Text.Length > 0)
            {
                stream = new MJPEGStream(txtIp.Text);
                stream.NewFrame += stream_Newframe;
                if (stream.IsRunning)
                {
                    stream.Stop();
                }
                else
                {
                    stream.Start();
                }
               
            }
            else
            {
                MessageBox.Show("Error: enter the IP address for cam !!");
            }
            
        }

        private void stream_Newframe(object sender, NewFrameEventArgs eventArgs)
        {
            Bitmap bitmap = (Bitmap)eventArgs.Frame.Clone();
            //Thread.Sleep(200);
            pictureBox2.Image = bitmap;
        }

        private void btnReadDB_Click(object sender, EventArgs e)
        {
            frmLogin frmLogin = new frmLogin();
            frmLogin.ShowDialog();
        }
    }
}
