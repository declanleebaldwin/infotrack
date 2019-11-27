using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace backend.Models
{
    public class Search
    {
        public string SearchTerm { get; set; }
        public string URL { get; set; }

        public string CreateGooglSearchURL ()
        {
            string trimmedSearchTerm = SearchTerm.Trim();
            return trimmedSearchTerm.Replace(' ', '+'); ;
        }
    }
}
