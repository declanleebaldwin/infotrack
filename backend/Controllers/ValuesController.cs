using System.Collections.Generic;
using backend.Models;
using Microsoft.AspNetCore.Mvc;
using HtmlAgilityPack;
using ScrapySharp.Extensions;
using System;
using System.Linq;
using ScrapySharp.Network;
using ScrapySharp.Html.Forms;
using GoogleScraper;
using System.Text.RegularExpressions;

namespace backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ValuesController : ControllerBase
    {
        // POST api/values
        [HttpPost]
        public List<int> Post([FromBody] Search search )
        {
            var googleSearchURL = search.CreateGooglSearchURL();
            var URL = search.URL;
            return GetListOfMatchedLinks(googleSearchURL, URL);
        }

        public static List<int> GetListOfMatchedLinks(string googleSearchURL, string searchPattern)
        {
            HttpSocket objHttpSocket = new HttpSocket();
            string sResult = objHttpSocket.GetHtml(new Uri(string.Format("https://www.google.com/search?num=100&q={0}", googleSearchURL)));
            string pattern = "(?s)<div class=\"g\".*?</div>";
            Regex rg = new Regex(pattern);
            MatchCollection links = rg.Matches(sResult);

            List<int> matchedLinksList = new List<int>();
            for (int count = 0; count < links.Count; count++)
            {
                Regex regex = new Regex(searchPattern);
                MatchCollection matchedLinks = regex.Matches(links[count].Value);
                if (matchedLinks.Count > 0)
                {
                    matchedLinksList.Add(count + 1);
                }
            }
            return matchedLinksList;
        }
    }
}
