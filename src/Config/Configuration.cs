using Dijon.Models;
using Newtonsoft.Json.Linq;
using System.IO;

namespace Dijon.Config
{
    public class Configuration
    {
       
        /// <summary>
        /// Read config.json from the directory where the bot is located
        /// </summary>
        /// <returns>A model containing the bot's client ID and token from the config file</returns>
        public static BotConfig ReadConfig()
        {

            if (!File.Exists("config.json"))
            {

                using (FileStream fileStream = new FileStream("config.json", FileMode.Create))
                {

                    using (StreamWriter writer = new StreamWriter(fileStream))
                    {

                        dynamic toWrite = new JObject();
                        toWrite.token = "default";
                        toWrite.clientid = "default";

                        writer.WriteLine(toWrite.ToString());

                    }
                }

                throw new IOException("config.json was not found!");

            }

            BotConfig config = new BotConfig();

            string configJSON = "";

            using (FileStream fileStream = new FileStream("config.json", FileMode.Open))
            {

                using (StreamReader reader = new StreamReader(fileStream))
                {

                    configJSON = reader.ReadToEnd();

                }

            }

            JObject json = JObject.Parse(configJSON);

            config.botToken = json["token"].ToString();
            config.clientID = json["clientid"].ToString();

            return config;

        }

    }
}