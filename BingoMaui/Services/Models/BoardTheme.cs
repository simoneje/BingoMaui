using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BingoMaui.Services.Models
{
    public class BoardTheme
    {
        public string PageBackground { get; set; } = "#FFFFFF";
        public string TileBackground { get; set; } = "#6D28D9"; // lila som du hade
        public string TileText { get; set; } = "#FFFFFF";
        public string BadgeBackground { get; set; } = "#00000099"; // svart med alpha
    }

}
