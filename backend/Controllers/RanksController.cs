using System.Collections.Generic;
using backend.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using GoogleScraper;
using System.Text.RegularExpressions;

namespace backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RanksController : ControllerBase
    {
        // POST api/ranks
        [HttpPost]
        public List<int> Post([FromBody] Search search )
        {
            var googleSearchURL = search.CreateGooglSearchURL();
            var URL = search.URL;
            return GetListOfGoogleRanks(googleSearchURL, URL);
        }

        public static List<int> GetListOfGoogleRanks(string googleSearchURL, string URL)
        {
            HttpSocket objHttpSocket = new HttpSocket();
            string html = objHttpSocket.GetHtml(new Uri(string.Format("https://www.google.com/search?num=100&q={0}", googleSearchURL)));
            string linkPattern = "(?s)<div class=\"g\".*?</div>";
            Regex linkRegex = new Regex(linkPattern);
            MatchCollection links = linkRegex.Matches(html);

            List<int> matchedLinksList = new List<int>();
            for (int count = 0; count < links.Count; count++)
            {
                Regex URLRegex = new Regex(URL);
                MatchCollection matchedLinks = URLRegex.Matches(links[count].Value);
                if (matchedLinks.Count > 0)
                {
                    matchedLinksList.Add(count + 1);
                }
            }
            return matchedLinksList;
        }
    }
}
