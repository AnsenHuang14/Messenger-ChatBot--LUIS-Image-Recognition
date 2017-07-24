using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using System;
using System.Linq;
using Microsoft.ProjectOxford.Vision;
using Microsoft.ProjectOxford.Emotion;
using Microsoft.Cognitive.LUIS;
using System.IO;
using System.Text;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;

using SpeechToText.Services;
using Imago.Facebook;
using Imago.Media;
using System.Web;
using System.Diagnostics;
using System.Net.Http.Headers;

namespace Bot_ChenYi
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        private readonly MicrosoftCognitiveSpeechService speechService = new MicrosoftCognitiveSpeechService();
        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            if (activity.Type == ActivityTypes.Message)
            {
                ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));
                Activity reply = activity.CreateReply();
                Activity reply_cat = activity.CreateReply();
                Activity reply_tag = activity.CreateReply();
                Activity reply_face = activity.CreateReply();
                Activity reply_emotion = activity.CreateReply();
                Activity reply_img = activity.CreateReply();
                Activity reply_bt = activity.CreateReply();
                //List<Activity> reply_ptt_list=null;
                //db
                var cb = new SqlConnectionStringBuilder();
                cb.DataSource = "nbadata.database.windows.net";
                cb.UserID = "nbadata";
                cb.Password = "GIIEnba123";
                cb.InitialCatalog = "NBADATA";
                //----s
                if (activity.Attachments?.Count > 0 && activity.Attachments.First().ContentType.StartsWith("image"))
                {
                    var url = activity.Attachments.First().ContentUrl;
                    //emotion api
                    EmotionServiceClient emo_client = new EmotionServiceClient("95a490338ba54f908c05e65d82e14b69", "https://westus.api.cognitive.microsoft.com/emotion/v1.0");
                    var emo_result = await emo_client.RecognizeAsync(url);
                    //辨識圖片
                    VisionServiceClient client = new VisionServiceClient("cf9176a0c5dd4784ad4cc3467a778924", "https://westcentralus.api.cognitive.microsoft.com/vision/v1.0");
                    //VisualFeature[] { VisualFeature.Description }矩陣列舉,在此指使用描述
                    var result = await client.AnalyzeImageAsync(url,new VisualFeature[] { VisualFeature.Description,VisualFeature.Categories,VisualFeature.Faces,VisualFeature.ImageType,VisualFeature.Tags });
                    var celebritiesResult = await client.AnalyzeImageInDomainAsync(url, "celebrities");
                    string tag_name = "Picture tags: "; 
                    foreach (var item in result.Tags)tag_name += item.Name.ToString()+ ",";
                    //string cat_name = "Categories of picture: ";
                    string cname = JObject.Parse(celebritiesResult.Result.ToString())["celebrities"].ToString();
                    if (cname.Length > 2) cname = JObject.Parse(JObject.Parse(celebritiesResult.Result.ToString())["celebrities"][0].ToString())["name"].ToString();
                    string cele = JObject.Parse(celebritiesResult.Result.ToString())["celebrities"].ToString();
                    if (cele.Length > 2) cele = "Probably " + JObject.Parse(JObject.Parse(celebritiesResult.Result.ToString())["celebrities"][0].ToString())["name"].ToString();
                    else cele = "unrecognized man";
                    //foreach (var item in result.Categories) cat_name += item.Name.ToString() + "\t";

                    /*string face = "Face in picture: ";
                    if (result.Faces.Length == 0) face += "No face.";
                    foreach (var item in result.Faces) face += "Gender: "+ item.Gender.ToString() + "\t"+"Age: "+item.Age.ToString()+"\n";
                    */
                    string emo = "Emotion detection: ";
                    if (emo_result.Length == 0) emo += "No face.";
                    foreach (var item in emo_result)
                    {
                        if (item.Scores.Anger > 0.01) emo += "Anger, ";
                        if (item.Scores.Contempt > 0.01) emo += "Contempt, ";
                        if (item.Scores.Disgust > 0.01) emo += "Disgust, ";
                        if (item.Scores.Fear > 0.01) emo += "Fear, ";
                        if (item.Scores.Happiness > 0.01) emo += "Happiness, ";
                        if (item.Scores.Neutral > 0.01) emo += "Neutral, ";
                        if (item.Scores.Sadness > 0.01) emo += "Sadness, ";
                        if (item.Scores.Surprise > 0.01) emo += "Surprise, ";
                    }
                    
                    
                    
                    reply.Text = "Description: "+result.Description.Captions.First().Text;
                    //reply_cat.Text = cat_name;
                    //reply_tag.Text = tag_name;
                    //reply_face.Text = face;
                    reply_emotion.Text = emo;
                    reply_img.Text = cele;
                    if (reply_img.Text!= "unrecognized man") Service(reply_bt, cname);

                }
                else
                {

                    if (activity.Text.Contains("&"))
                    {
                        //reply.Text = activity.Text.Substring(0, activity.Text.IndexOf('-'));
                        switch (activity.Text.Substring(0, activity.Text.IndexOf('&')))
                        {
                            case "PTT":
                                //reply.Text = activity.Text.Substring(activity.Text.IndexOf('&') +1 , (activity.Text.Length - activity.Text.IndexOf('&') -1));
                                // select ptt by name
                                using (var connection = new SqlConnection(cb.ConnectionString))
                                {
                                    connection.Open();
                                    reply.Text = Submit_PTT_Select(connection, activity.Text.Substring(activity.Text.IndexOf('&') + 1, (activity.Text.Length - activity.Text.IndexOf('&') - 1)));

                                }
                                break;
                            case "career stat":
                                //reply.Text = activity.Text.Substring(activity.Text.IndexOf('&') +1 , (activity.Text.Length - activity.Text.IndexOf('&') -1));
                                // select stat by name
                                using (var connection = new SqlConnection(cb.ConnectionString))
                                {
                                    connection.Open();
                                    reply.Text = Submit_Player_Stat_Select(connection,activity.Text.Substring(activity.Text.IndexOf('&') + 1, (activity.Text.Length - activity.Text.IndexOf('&') - 1)));
                                }
                                break;
                            default:
                                reply.Text = activity.Text;
                                break;
                        }
                    }
                    else
                    {
                        /*string message = string.Empty;
                        try
                        {
                            var audioAttachment2 = activity.Attachments?.FirstOrDefault(a => a.ContentType.Equals("video/mp4") || a.ContentType.Contains("audio") || a.ContentType.Contains("video"));
                            var audioAttachment = activity.Attachments?.FirstOrDefault(a => a.ContentType.Equals("audio/wav") || a.ContentType.Equals("application/octet-stream"));
                            if (audioAttachment != null)
                            {
                                
                                var stream = await GetAudioStream(connector, audioAttachment);
                                var text = await this.speechService.GetTextFromAudioAsync(stream);
                                message = ProcessText(text);
                                reply = activity.CreateReply(message);
                                await connector.Conversations.ReplyToActivityAsync(reply);

                            }
                            else if (audioAttachment2 != null)
                            {
                               
                                IncomingFacebookVoiceMessage voice = new IncomingFacebookVoiceMessage(activity);
                                try
                                {

                                    if (voice != null)
                                    {

                                        //Download original MP4 voice message
                                        voice.DownloadFile();
                                        throw new Exception("ddffdfdfdfdfdfdfdfdffdf");
                                        var mp4 = voice.GetLocalPathAndFileName();
                                     
                                        //Convert MP4 to WAV
                                        var wavFolder = "D:" + @"\" + "home" + @"\" + "site" + @"\" + "wwwroot" + @"\" + "bin" + @"\" + "en";
                                        var converter = new AudioFileFormatConverter(mp4, wavFolder);
                                        var wav = converter.ConvertMP4ToWAV();


                                        //Convert .WAV file to text
                                        var bing = new MicrosoftCognitiveSpeechService(); //gets the path + filename



                                        // convert string to stream

                                        byte[] data = File.ReadAllBytes(wav);
                                        //byte[] byteArray = Encoding.ASCII.GetBytes(contents);
                                        MemoryStream stream = new MemoryStream(data);





                                        var text = await this.speechService.GetTextFromAudioAsync(stream); //takes path+filename to WAV file, returns text

                                        if (string.IsNullOrWhiteSpace(text))
                                        {
                                            message = "Looks like you didn't say anything.";
                                        }
                                        else
                                        {
                                            message = text;
                                        }

                                        //Clean up files from disk
                                        voice.RemoveFromDisk();
                                        converter.RemoveTargetFileFromDisk();
                                    }

                                }
                                catch (Exception ex)
                                {
                                    message = "Woah! " + ex.Message.Trim().Trim('.') + "!";
                                }



                            }
                            else
                            {
                                message = "Did you upload an audio file? I'm more of an audible person. Try sending me a wav file";
                            }
                        }
                        catch (Exception e)
                        {
                            message = "Oops! Something went wrong. Try again later";
                            if (e is HttpException)
                            {
                                var httpCode = (e as HttpException).GetHttpCode();
                                if (httpCode == 401 || httpCode == 403)
                                {
                                    message += $" [{e.Message} - hint: check your API KEY at web.config]";
                                }
                                else if (httpCode == 408)
                                {
                                    message += $" [{e.Message} - hint: try send an audio shorter than 15 segs]";
                                }
                            }

                            Trace.TraceError(e.ToString());
                        }
                        */
                        //-----------------------------------------

                        using (LuisClient client = new LuisClient("16f5717a-ee86-4074-abf4-603c9d6cd733", "c74954db4ddb4000bb6865cf07c2ad36"))
                        {


                            var result = await client.Predict(activity.Text);

                            List<string> Teams = new List<string>();
                            List<string> Date = new List<string>();
                            string Players = "", temp, teamdate = "", playerdate = "";

                            List<string> OUTPUT = new List<string>();// team team date



                            foreach (var OneItem in result.Entities)
                            {
                                foreach (var Item in OneItem.Value)
                                {
                                    //reply.Text += Item.ParentType + "   " + Item.Value + "\n";
                                    //reply.Text += Item.Name + "   " + Item.Value + "   " + Item.Score + "   ";
                                    foreach (var r in Item.Resolution)
                                    {
                                        //reply.Text += r.Value.ToString() + "  \n";
                                        temp = r.Value.ToString();

                                        if (Item.Name == "Teams")
                                            Teams.Add(temp);
                                        else if (Item.Name == "Players")
                                            Players = temp;
                                        else if (Item.Name == "builtin.number")
                                            Date.Add(temp);
                                        //    Teams.Add(r.Value.ToString());
                                    }
                                }
                                //reply.Text += "Key = " + OneItem.Key + ", Value = " + OneItem.Value[0].Value + "\n";
                            }

                            //string[] DATE = Date.ToArray();
                            //reply.Text += Date.Count;
                            int digit = 1;
                            if (Date.Count > 1)
                            {
                                if (Date.Count == 2)
                                {
                                    teamdate += "2017";
                                    playerdate += "2017";
                                    digit = 0;
                                }
                                else
                                {
                                    teamdate += Date[0];
                                    playerdate += Date[0];
                                }
                                int tem;
                                for (int i = digit; i < Date.Count; i++)
                                {
                                    tem = Convert.ToInt32(Date[i]);
                                    if (tem < 10)
                                    {
                                        temp = Convert.ToString(tem);
                                        playerdate += "/" + temp;
                                        teamdate += "/0" + temp;
                                    }
                                    else
                                    {
                                        playerdate += "/" + Date[i];
                                        teamdate += "/" + Date[i];
                                    }
                                }

                            }
                            //reply.Text += result.TopScoringIntent.Name;

                            if (result.TopScoringIntent.Name == "Games" && teamdate != "")
                            {
                                for (int i = 0; i < Teams.Count; i++)
                                {
                                    Teams[i] = Teams[i].Remove(Teams[i].Length - 4, 4);
                                    Teams[i] = Teams[i].Remove(0, 6);
                                    OUTPUT.Add(Teams[i]);
                                    //reply.Text += Teams[i] + " ";
                                }
                                //reply.Text += teamdate;
                                OUTPUT.Add(teamdate);
                                //////////input Teams & Date
                            }
                            else if (result.TopScoringIntent.Name == "Personal Performance")
                            {
                                Players = Players.Remove(Players.Length - 4, 4);
                                Players = Players.Remove(0, 6);
                                //reply.Text += Players + playerdate;
                                OUTPUT.Add(Players);
                                if (playerdate != "")
                                    OUTPUT.Add(playerdate);
                                /////////input Players & Date
                            }
                            //else
                            //    reply.Text = "Please enter more details.";
                            //reply.Text += OUTPUT.Count;
                            switch (OUTPUT.Count)
                            {
                                case 3://team+team+date
                                    //reply.Text += OUTPUT[0];
                                    //reply.Text += OUTPUT[1];
                                    //reply.Text += OUTPUT[2];
                                    using (var connection = new SqlConnection(cb.ConnectionString))
                                    {
                                        connection.Open();
                                        //reply.Text = OUTPUT[2]+OUTPUT[0]+ OUTPUT[1] ;
                                         reply.Text = Submit_Team_Select(connection, OUTPUT[2], OUTPUT[0], OUTPUT[1]);
                                    }
                                    break;
                                case 2://player+date
                                    //reply.Text += OUTPUT[0];
                                    //reply.Text += OUTPUT[1];
                                    using (var connection = new SqlConnection(cb.ConnectionString))
                                    {
                                        connection.Open();
                                        reply.Text = Submit_Player_Stat_Select(connection, OUTPUT[0], OUTPUT[1]);
                                    }
                                    break;
                                case 1://player
                                    //reply.Text += OUTPUT[0];
                                    Service(reply_bt, OUTPUT[0]);
                                    break;
                                default:
                                    reply.Text = "Please enter more details.";
                                    break;
                            }
                            //reply.Text = team1 + team2 + player + dt;

                        }
                    }
                    

                   
                }



                await connector.Conversations.ReplyToActivityAsync(reply);
                //await connector.Conversations.ReplyToActivityAsync(reply_cat);
                /*await connector.Conversations.ReplyToActivityAsync(reply_tag);
                await connector.Conversations.ReplyToActivityAsync(reply_face);*/
                await connector.Conversations.ReplyToActivityAsync(reply_emotion);
                await connector.Conversations.ReplyToActivityAsync(reply_img);
                await connector.Conversations.ReplyToActivityAsync(reply_bt);
                /*foreach (var item in reply_ptt_list)
                {
                    await connector.Conversations.ReplyToActivityAsync(item);
                }*/

            }



            var response = Request.CreateResponse(HttpStatusCode.OK);
            return response;
        }

        private Activity HandleSystemMessage(Activity message)
        {
            if (message.Type == ActivityTypes.DeleteUserData)
            {
                // Implement user deletion here
                // If we handle user deletion, return a real message
            }
            else if (message.Type == ActivityTypes.ConversationUpdate)
            {
                // Handle conversation state changes, like members being added and removed
                // Use Activity.MembersAdded and Activity.MembersRemoved and Activity.Action for info
                // Not available in all channels
            }
            else if (message.Type == ActivityTypes.ContactRelationUpdate)
            {
                // Handle add/remove from contact lists
                // Activity.From + Activity.Action represent what happened
            }
            else if (message.Type == ActivityTypes.Typing)
            {
                // Handle knowing tha the user is typing
            }
            else if (message.Type == ActivityTypes.Ping)
            {
            }

            return null;
        }

        private void Service(Activity reply,string cele)
        {
            List<Attachment> att = new List<Attachment>();
            string bt1 = "PTT&" + cele;
            string bt2 = "career stat&" + cele;
            att.Add(new ThumbnailCard()
            {
                Title = "Want to know...",
                Buttons = new List<CardAction>()
                        {
                            new CardAction(ActionTypes.PostBack, "PTT News",value:bt1),
                            new CardAction(ActionTypes.PostBack, "Player Statistic",value:bt2)
                        }
            }.ToAttachment());
            /*att.Add(new HeroCard()
            {
                Title = "Surface Pro",
                //Images = new List<CardImage>() { new CardImage("https://s.yimg.com/wb/images/268917ABD27238C9A20428002A8143AEEF40A048") },
                Buttons = new List<CardAction>()
                        {
                            new CardAction(ActionTypes.OpenUrl, "Yahoo購物中心", value: $"https://tw.buy.yahoo.com/gdsale/gdsale.asp?act=gdsearch&gdid=6561885")
                        }
            }.ToAttachment());*/
            
            reply.AttachmentLayout = AttachmentLayoutTypes.Carousel;
            reply.Attachments = att;
        }
        static string Submit_PTT_Select(SqlConnection connection, string name)
        {
            string tsql = Build_PTT_Tsql_Select(name);
            string news = "";
            using (var command = new SqlCommand(tsql, connection))
            {
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                      
                        while (reader.Read())
                        {
                            //Activity act = activity.CreateReply();
                            //reply_ptt_list.Add(act);
                            //act.Text = reader.GetString(1) + "-" + reader.GetString(0)+"\n" + reader.GetString(2);
                            news += reader.GetString(1)+"-";
                            news += reader.GetString(0)+"\n";
                            news += reader.GetString(2)+"\n";

                        }
                    }
                }
            }
            return news;
        }

        static string Build_PTT_Tsql_Select(string name)
        {
            return @"
			SELECT TOP 10 pttNBA.title, pttNBA.dt , pttNBA.link FROM pttNBA
			WHERE pttNBA.title LIKE " + "'%" + name + "%' ORDER BY dt DESC";
        }

        static string Submit_Player_Stat_Select(SqlConnection connection, string name, string date = null)
        {
            string news = "";
            if (date == null)
            {
                string tsq1 = Build_Player_stat_Select(name);
                using (var command = new SqlCommand(tsq1, connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string t = " avg_blocks, avg_field_goals_made, avg_field_goals_pct, avg_free_throws_made, avg_free_throws_pct, avg_offensive_rebounds, avg_rebounds, avg_points, avg_steals, avg_three_points_made, avg_three_points_pct, location, avg_two_points_made, avg_two_points_pct";
                            string[] title = t.Split(',');
                            int i = reader.FieldCount;
                            for (int a = 0; a < i; a++)
                            {
                                news += title[a] + " : "+reader[a].ToString() + "\n";
                            }
                        }

                    }
                }
                return news;
            }
            else
            {
                string tsql = Build_Player_stat_Select(name,date);
                using (var command = new SqlCommand(tsql, connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        string t = "blocks,field_goals_made,field_goals_pct,free_throws_made,free_throws_pct,offensive_rebounds,rebounds,points,steals,three_points_made,three_points_pct,team,location,two_points_made,two_points_pct,injuries,jersey_number,last_name,position,primary_position,starter";
                        string[] title = t.Split(',');
                        while (reader.Read())
                        {
                            int i = reader.FieldCount;
                            for (int a = 0; a < i; a++)
                            {
                                news += title[a] + " : " + reader[a].ToString() + "\n";
                            }
                        }

                    }
                }
                
                return news;
            }
            

        }

        static string Build_Player_stat_Select(string name)
        {
            return @"
					select 
		avg(convert(float,player_stat.blocks)) blocks
        , avg(convert(float,player_stat.field_goals_made)) field_goals_made
		, avg(convert(float,player_stat.field_goals_pct)) field_goals_pct
		, avg(convert(float,player_stat.free_throws_made )) free_throws_made
		, avg(convert(float,player_stat.free_throws_pct)) free_throws_pct
		, avg(convert(float,player_stat.offensive_rebounds)) offensive_rebounds
		, avg(convert(float,player_stat.rebounds)) rebounds
		, avg(convert(float,player_stat.points)) points
		, avg(convert(float,player_stat.steals)) steals 
		, avg(convert(float,player_stat.three_points_made)) three_points_made
		, avg(convert(float,player_stat.three_points_pct)) three_points_pct
		, avg(convert(float,player_stat.two_points_made)) two_points_made
		, avg(convert(float,player_stat.two_points_pct)) two_points_pct 
			from player_stat Where player_stat.full_name ='" + name + "'";
        }

        static string Build_Player_stat_Select(string name, string date)
        {
            return @"
		select 
		player_stat.blocks
        , player_stat.field_goals_made
		, player_stat.field_goals_pct
		, player_stat.free_throws_made 
		, player_stat.free_throws_pct
		, player_stat.offensive_rebounds
		, player_stat.rebounds
		, player_stat.points
		, player_stat.steals
		, player_stat.three_points_made
		, player_stat.three_points_pct
		, player_stat.team
		, player_stat.location
		, player_stat.two_points_made
		, player_stat.two_points_pct
		, player_stat.injuries
		, player_stat.jersey_number
		, player_stat.last_name
		, player_stat.position
		, player_stat.primary_position
		, player_stat.starter

		from player_stat Where player_stat.full_name = '" + name + "' and player_stat.dt = '" + date + "'";
        }

        static string Submit_Team_Select(SqlConnection connection, string name1, string name2 = null, string date = null)
        {
            string news = "";
            if (name2 == null && date == null)
            {
                string tsql = Build_Team_Select(name1);
                List<string> data = new List<string>();
                using (var command = new SqlCommand(tsql, connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        string t = "avg_blocks,avg_field_goals_made,avg_field_goals_pct,avg_free_throws_made ,avg_free_throws_pct,avg_offensive_rebounds,avg_rebounds,avg_points,avg_steals,avg_three_points_made,avg_three_points_pct,location,avg_lead_changes,avg_two_points_made,avg_two_points_pct";
                        string[] title = t.Split(',');
                        while (reader.Read())
                        {
                            int i = reader.FieldCount;
                            for (int a = 0; a < i; a++)
                            {
                                news += title[a] + " : " + reader[a].ToString() + "\n";
                                //Console.Write(title[a] + " : " + reader[a].ToString() + "\n");
                            }
                        }

                    }
                }
                return news;
            }
            else
            {
                string tsql = Build_Team_Select(name1, name2, date);

                using (var command = new SqlCommand(tsql, connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        string t = "assists,blocks,field_goals_pct,points,steals,team_rebounds,turnovers,dt,team";
                        string[] title = t.Split(',');


                        while (reader.Read())
                        {
                            int i = reader.FieldCount;
                            for (int a = 0; a < i; a++)
                            {
                                news += title[a] + " : " + reader[a].ToString() + "\n";
                                //Console.Write(title[a] + " : " + reader[a].ToString() + "\n");
                            }
                        }


                    }
                }
                return news;

            }



        }


        static string Build_Team_Select(string date, string name1, string name2 = null)
        {
            return @"
			select 
				team.assists
				, team.blocks
				, team.field_goals_pct
				, team.points
				, team.steals
				, team.team_rebounds
				, team.turnovers
				, team.dt
				, team.team
		from team Where team.dt = '" + date + "' and " + "(team.team = '" + name1 + "' OR team.team = '" + name2 + "')";
        }

        static string Build_Team_Select(string name)
        {
            return @"select avg(convert(float,team.blocks))
				, avg(convert(float,team.field_goals_made))
				, avg(convert(float,team.field_goals_pct))
				, avg(convert(float,team.free_throws_made ))
				, avg(convert(float,team.free_throws_pct))
				, avg(convert(float,team.offensive_rebounds))
				, avg(convert(float,team.rebounds))
				, avg(convert(float,team.points))
				, avg(convert(float,team.steals))
				, avg(convert(float,team.three_points_made))
				, avg(convert(float,team.three_points_pct))
				, team.location
				, avg(convert(float,team.lead_changes))
				, avg(convert(float,team.two_points_made))
				, avg(convert(float,team.two_points_pct)) from team Where team.team = '" + name + "'group by team.location";
        }

        private static string ProcessText(string text)
        {
            string message = "You said : " + text + ".";

            if (!string.IsNullOrEmpty(text))
            {
                var wordCount = text.Split(' ').Count(x => !string.IsNullOrEmpty(x));
                message += "\n\nWord Count: " + wordCount;

                var characterCount = text.Count(c => c != ' ');
                message += "\n\nCharacter Count: " + characterCount;

                var spaceCount = text.Count(c => c == ' ');
                message += "\n\nSpace Count: " + spaceCount;

                var vowelCount = text.ToUpper().Count("AEIOU".Contains);
                message += "\n\nVowel Count: " + vowelCount;

            }

            return message;
        }

        private static async Task<Stream> GetAudioStream(ConnectorClient connector, Attachment audioAttachment)
        {
            using (var httpClient = new HttpClient())
            {
                // The Skype attachment URLs are secured by JwtToken,
                // you should set the JwtToken of your bot as the authorization header for the GET request your bot initiates to fetch the image.
                // https://github.com/Microsoft/BotBuilder/issues/662
                var uri = new Uri(audioAttachment.ContentUrl);
                if (uri.Host.EndsWith("skype.com") && uri.Scheme == "https")
                {
                    throw new Exception("jjjjjjjj");
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await GetTokenAsync(connector));
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/octet-stream"));
                }

                return await httpClient.GetStreamAsync(uri);
            }
        }

        private static async Task<string> GetTokenAsync(ConnectorClient connector)
        {
            var credentials = connector.Credentials as MicrosoftAppCredentials;
            if (credentials != null)
            {
                return await credentials.GetTokenAsync();
            }

            return null;
        }
    }
}