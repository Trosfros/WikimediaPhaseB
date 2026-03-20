using DAL;
using System.Collections.Generic;
using System.Linq;

namespace Models
{
    public class MediasRepository : Repository<Media>
    {
        // On ne surcharge plus ToList() ici, car Media.cs s'en occupe déjà via son 'get'

        public List<string> MediasCategories()
        {
            List<string> Categories = new List<string>();
            // ToList() de la classe de base suffit amplement
            foreach (Media media in ToList().OrderBy(m => m.Category))
            {
                if (Categories.IndexOf(media.Category) == -1)
                {
                    Categories.Add(media.Category);
                }
            }
            return Categories;
        }
    }
}