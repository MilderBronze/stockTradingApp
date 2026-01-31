using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace api.Models
{
    [Table("Portfolios")]
    public class Portfolio
    {
        public string AppUserId { get; set; }
        public int StockId { get; set; }

        // and now the navigation properties: these are just for the developers.
        public AppUser AppUser { get; set; }
        public Stock Stock { get; set; }
    }
}
