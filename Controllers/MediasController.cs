using DAL;
using Models;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using static Controllers.AccessControl;

[UserAccess(Access.View)]
public class MediasController : Controller
{

    private void InitSessionVariables()
    {
        // Session is a dictionary that hold keys values specific to a session
        // Each user of this web application have their own Session
        // A Session has a default time out of 20 minutes, after time out it is cleared

        if (Session["CurrentMediaId"] == null) Session["CurrentMediaId"] = 0;
        if (Session["CurrentMediaTitle"] == null) Session["CurrentMediaTitle"] = "";
        if (Session["Search"] == null) Session["Search"] = false;
        if (Session["SearchString"] == null) Session["SearchString"] = "";
        if (Session["SelectedCategory"] == null) Session["SelectedCategory"] = "";
        if (Session["Categories"] == null) Session["Categories"] = DB.Medias.MediasCategories();
        if (Session["SortByTitle"] == null) Session["SortByTitle"] = true;
        if (Session["SortAscending"] == null) Session["SortAscending"] = true;
        ValidateSelectedCategory();
    }

    private void ResetCurrentMediaInfo()
    {
        Session["CurrentMediaId"] = 0;
        Session["CurrentMediaTitle"] = "";
    }

    private void ValidateSelectedCategory()
    {
        if (Session["SelectedCategory"] != null)
        {
            var selectedCategory = (string)Session["SelectedCategory"];
            var Medias = DB.Medias.ToList().Where(c => c.Category == selectedCategory);
            if (Medias.Count() == 0)
                Session["SelectedCategory"] = "";
        }
    }

    public ActionResult GetMediasCategoriesList(bool forceRefresh = false)
    {
        try
        {
            InitSessionVariables();

            bool search = (bool)Session["Search"];

            if (search)
            {
                return PartialView();
            }
            return null;
        }
        catch (System.Exception ex)
        {
            return Content("Erreur interne" + ex.Message, "text/html");
        }
    }
    // This action produce a partial view of Medias
    // It is meant to be called by an AJAX request (from client script)
    public ActionResult GetMediaDetails(bool forceRefresh = false)
    {
        try
        {
            InitSessionVariables();
            int mediaId = (int)Session["CurrentMediaId"];
            Media Media = DB.Medias.Get(mediaId);

            if (Media != null)
            {
                // IMPORTANT : On retire le "if (canView)" ici pour laisser 
                // la VUE (.cshtml) gérer l'affichage du message rouge.
                if (DB.Users.HasChanged || DB.Medias.HasChanged || forceRefresh)
                {
                    return PartialView(Media); // On envoie toujours le média
                }
            }
            return null;
        }
        catch (System.Exception ex)
        {
            return Content("Erreur interne" + ex.Message, "text/html");
        }
    }
    public ActionResult GetMedias(bool forceRefresh = false)
    {
        try
        {
            if (DB.Medias.HasChanged || forceRefresh)
            {
                InitSessionVariables();
                IEnumerable<Media> result = null;
                var currentUser = Models.User.ConnectedUser;

                // 1. Récupération et Filtrage (B.29)
                var allMedias = DB.Medias.ToList();
                // var currentUser = Models.User.ConnectedUser; <--- Tu peux supprimer celle-là, elle est déjà au-dessus
                result = allMedias.Where(m => m.Shared ||
                                             (currentUser != null && (m.OwnerId == currentUser.Id || currentUser.IsAdmin)));

                // 2. Recherche
                bool search = (bool)Session["Search"];
                if (search)
                {
                    string searchString = (string)Session["SearchString"];
                    result = result.Where(c => c.Title.ToLower().Contains(searchString));

                    string SelectedCategory = (string)Session["SelectedCategory"];
                    if (!string.IsNullOrEmpty(SelectedCategory))
                        result = result.Where(c => c.Category == SelectedCategory);
                }

                // 3. Tri
                if ((bool)Session["SortAscending"])
                {
                    result = (bool)Session["SortByTitle"] ? result.OrderBy(c => c.Title) : result.OrderBy(c => c.PublishDate);
                }
                else
                {
                    result = (bool)Session["SortByTitle"] ? result.OrderByDescending(c => c.Title) : result.OrderByDescending(c => c.PublishDate);
                }

                return PartialView(result);
            }
            return null;
        }
        catch (System.Exception ex)
        {
            return Content("Erreur interne : " + ex.Message, "text/html");
        }
    }


    public ActionResult List()
    {
        ResetCurrentMediaInfo();
        return View();
    }

    public ActionResult ToggleSearch()
    {
        if (Session["Search"] == null) Session["Search"] = false;
        Session["Search"] = !(bool)Session["Search"];
        return RedirectToAction("List");
    }
    public ActionResult SortByTitle()
    {
        Session["SortByTitle"] = true;
        return RedirectToAction("List");
    }
    public ActionResult ToggleSort()
    {
        Session["SortAscending"] = !(bool)Session["SortAscending"];
        return RedirectToAction("List");
    }
    public ActionResult SortByDate()
    {
        Session["SortByTitle"] = false;
        return RedirectToAction("List");
    }

    public ActionResult SetSearchString(string value)
    {
        Session["SearchString"] = value.ToLower();
        return RedirectToAction("List");
    }

    public ActionResult SetSearchCategory(string value)
    {
        Session["SelectedCategory"] = value;
        return RedirectToAction("List");
    }
    public ActionResult About()
    {
        return View();
    }


    public ActionResult Details(int id)
    {
        Session["CurrentMediaId"] = id;
        Media Media = DB.Medias.Get(id);
        if (Media != null)
        {
            Session["CurrentMediaTitle"] = Media.Title;
            return View(Media);
        }
        return RedirectToAction("List");
    }
    [UserAccess(Access.Write)]
    public ActionResult Create()
    {
        return View(new Media());
    }

    [HttpPost]
    [UserAccess(Access.Write)]
    /* Install anti forgery token verification attribute.
     * the goal is to prevent submission of data from a page 
     * that has not been produced by this application*/
    [ValidateAntiForgeryToken()]
    public ActionResult Create(Media Media)
    {
        if (ModelState.IsValid)
        {
            // On récupère l'ID de l'utilisateur connecté via ton modèle User.cs
            if (Models.User.ConnectedUser != null)
            {
                Media.OwnerId = Models.User.ConnectedUser.Id;
            }

            DB.Medias.Add(Media);
            return RedirectToAction("List");
        }
        return View(Media);
    }

    [UserAccess(Access.Write)]
    public ActionResult Delete()
    {
        int id = Session["CurrentMediaId"] != null ? (int)Session["CurrentMediaId"] : 0;
        if (id != 0)
        {
            Media media = DB.Medias.Get(id);
            var currentUser = Models.User.ConnectedUser;

            if (media != null && currentUser != null)
            {
                if (media.OwnerId == currentUser.Id || currentUser.IsAdmin)
                {
                    DB.Medias.Delete(id);
                    return RedirectToAction("List");
                }

                // Tentative d'accès illégal par URL
                return RedirectToAction("Logout", "Accounts", new { message = "Accès illégal!" });
            }
        }
        return RedirectToAction("List");
    }

    [UserAccess(Access.Write)]
    public ActionResult Edit()
    {
        int id = Session["CurrentMediaId"] != null ? (int)Session["CurrentMediaId"] : 0;
        if (id != 0)
        {
            Media media = DB.Medias.Get(id);
            var currentUser = Models.User.ConnectedUser;

            if (media != null && currentUser != null)
            {
                // Si c'est le proprio OU l'admin, on laisse passer
                if (media.OwnerId == currentUser.Id || currentUser.IsAdmin)
                    return View(media);

                // SINON : Tentative d'accès illégal
                return RedirectToAction("Logout", "Accounts", new { message = "Accès illégal!" });
            }
        }
        return RedirectToAction("List");
    }

    [UserAccess(Access.Write)]
    [HttpPost]
    [ValidateAntiForgeryToken()]
    public ActionResult Edit(Media Media)
    {
        int id = Session["CurrentMediaId"] != null ? (int)Session["CurrentMediaId"] : 0;
        Media storedMedia = DB.Medias.Get(id);

        // AJOUT DE LA CONDITION ADMIN ICI
        if (storedMedia != null && Models.User.ConnectedUser != null)
        {
            if (storedMedia.OwnerId == Models.User.ConnectedUser.Id || Models.User.ConnectedUser.IsAdmin)
            {
                Media.Id = id;
                // On garde le OwnerId original de la base de données, même si c'est l'admin qui édite
                Media.OwnerId = storedMedia.OwnerId;
                Media.PublishDate = storedMedia.PublishDate;
                DB.Medias.Update(Media);
                return RedirectToAction("Details/" + id);
            }
        }
        return RedirectToAction("List");
    }

    // This action is meant to be called by an AJAX request
    // Return true if there is a name conflict
    // Look into validation.js for more details
    // and also into Views/Medias/MediaForm.cshtml
    public JsonResult CheckConflict(string YoutubeId)
    {
        int id = Session["CurrentMediaId"] != null ? (int)Session["CurrentMediaId"] : 0;
        // Response json value true if name is used in other Medias than the current Media
        return Json(DB.Medias.ToList().Where(c => c.YoutubeId == YoutubeId && c.Id != id).Any(),
                    JsonRequestBehavior.AllowGet /* must have for CORS verification by client browser */);
    }

}
