using DAL;
using Newtonsoft.Json; 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Models
{
    public class Media : Record
    {
        public string Title { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }
        public string YoutubeId { get; set; }
        public DateTime PublishDate { get; set; } = DateTime.Now;

        public int OwnerId { get; set; }

        public bool Shared { get; set; }

        // Propriété pratique pour accéder facilement aux infos du créateur (avatar, nom)
        [JsonIgnore]
        public User Owner
        {
            get
            {
                return DB.Users.Get(OwnerId);
            }
        }
    }
}