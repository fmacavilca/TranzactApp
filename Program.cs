using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace TranzactApp
{
    class Program
    {


        static void Main(string[] args)
        {
            Console.WriteLine("Loading...");
            GetDataFromWikiMedia();          
        }

        
        public static void GetDataFromWikiMedia()
        {
            string url = "https://dumps.wikimedia.org/other/pageviews/";
            DateTime today = DateTime.Now;
            string year = today.Year.ToString();
            string yearm = today.Year.ToString() + "-" + today.Month.ToString().PadLeft(2, '0');
            int hours = -5;
            string pattern = ".gz";
            string regex1 = "<a href=\".*\">(?<name>.*)</a>";
            string regex2 = "<a href=\".*\">(?<name>.*)</a>(?<value3>.*)";

            //Step 1: Get today Year and search in first page 
            string html = GetHtmlLinkString(url);
            string yearfound = FromHtmlgetURl(year, regex1, html);
            string htmlyeardate = GetHtmlLinkString(url + yearfound);

            //Step 2: Get Today Year-Month search in webpage tag <a>
            string yearmfound = FromHtmlgetURl(yearm, regex1, htmlyeardate);
            string htmlzip = GetHtmlLinkString(url + yearfound + yearmfound);

            //Step 3: Now We need to get data into table and filter by Date>'N' depending of last file hours operate '-5' hours
            List<DirectoryListTemp> TempListFromWeb = GetListFromWeb(pattern, regex2, htmlzip);
            string finalurl = url + yearfound + yearmfound;
            var test_t = TempListFromWeb.OrderByDescending(x => x.time).First();
            TempListFromWeb = TempListFromWeb.Where(x => Convert.ToDateTime(x.time) > Convert.ToDateTime(test_t.time).AddHours(hours)).ToList();

            //Step 4: We have the zips for download and put all data into List then just calculate COUNT_VIEWS
            List<All_Hours> Final_list = GetFinalData(TempListFromWeb, finalurl, today);

            //Print Result
            foreach (All_Hours var in Final_list)
            {
                Console.WriteLine(var.DOMAIN_CODE + " " + var.PAGETITLE + " " + var.COUNTS_VIEWS);           

            }

        }
        public static string GetHtmlLinkString(string url)
        {
            StreamReader reader =null;
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                reader = new StreamReader(response.GetResponseStream());

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            
            return reader.ReadToEnd();

        }
        public static string FromHtmlgetURl(string condition, string regexpattern, string html)
        {
            Regex regex = new Regex(regexpattern);
            MatchCollection matches = regex.Matches(html);
            string result = "";
            if (matches.Count > 0)
            {
                foreach (Match match in matches)
                {

                    if (match.Success)
                    {
                        //Console.WriteLine(match.Groups[1].Value);
                        if (match.Groups[1].Value.Contains(condition))
                            result= match.Groups[1].Value;
                    }

                }
            }
            return result;

        }

        public static List<DirectoryListTemp> GetListFromWeb(string condition, string regexpattern, string html)
        {
            Regex regex = new Regex(regexpattern);
            MatchCollection matches = regex.Matches(html);
            List<DirectoryListTemp> result = new List<DirectoryListTemp>();
            
            if (matches.Count > 0)
            {
                foreach (Match match in matches)
                {
                    DirectoryListTemp temp = new DirectoryListTemp();
                    if (match.Success)
                    {                        
                        if (match.Groups[1].Value.Contains(condition))
                        {
                            temp.zip = match.Groups[1].Value;
                            temp.time = match.Groups[2].Value.TrimStart().Split(' ')[0]+" "+ match.Groups[2].Value.TrimStart().Split(' ')[1];
                            temp.hour=match.Groups[2].Value.TrimStart().Split(' ')[1];
                            result.Add(temp);
                           
                        }
                            
                    }
                    
                }
            }
            
            return result;
           
        }

        public static List<All_Hours> GetFinalData(List<DirectoryListTemp> obj, string url, DateTime today)
        {
            List<All_Hours> result = new List<All_Hours>();
            List<All_Hours> temp_ListAllHour = new List<All_Hours>();
            Dictionary<string, All_Hours> test_1 = new Dictionary<string, All_Hours>();
            try
            {
                foreach (DirectoryListTemp var in obj)
                {
                    WebRequest request = (HttpWebRequest)WebRequest.Create(url + var.zip);
                    WebResponse response = request.GetResponse();
                    Stream responseStream = response.GetResponseStream();
                    GZipStream zipStream = new GZipStream(responseStream, CompressionMode.Decompress);
                    var outputStream = new MemoryStream();
                    zipStream.CopyTo(outputStream);
                    byte[] outputBytes = outputStream.ToArray();
                    var file = Encoding.ASCII.GetString(outputBytes);
                    var reader = new StringReader(file);

                    for (string line = reader.ReadLine(); line != null; line = reader.ReadLine())
                    {
                        All_Hours tmp_allhours = new All_Hours();
                        tmp_allhours.DOMAIN_CODE = line.Split(' ')[0];
                        tmp_allhours.PAGETITLE = line.Split(' ')[1];
                        tmp_allhours.COUNTS_VIEWS = Convert.ToInt32(line.Split(' ')[2]);                        
                        temp_ListAllHour.Add(tmp_allhours);
                    }
                }               
                var final_data = temp_ListAllHour
                    .GroupBy(d => new { d.DOMAIN_CODE, d.PAGETITLE })
                    .Select(x => new { x.Key.DOMAIN_CODE, x.Key.PAGETITLE, COUNTS_VIEWS = x.Sum(s => s.COUNTS_VIEWS) })
                    .OrderByDescending(x => x.COUNTS_VIEWS)
                    .Take(100)
                    .ToList();               

                foreach (var val in final_data)
                {
                    All_Hours tmp_2 = new All_Hours();
                    tmp_2.DOMAIN_CODE = val.DOMAIN_CODE;
                    tmp_2.PAGETITLE = val.PAGETITLE;
                    tmp_2.COUNTS_VIEWS = val.COUNTS_VIEWS;
                    result.Add(tmp_2);

                }
            }

            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            
            return result;
        }
    }
}
