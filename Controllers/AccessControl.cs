using Models;
using System;
using System.Web;
using System.Web.Mvc;

namespace Controllers
{

    public class AccessControl
    {

        public class UserAccess : AuthorizeAttribute
        {
            private Access RequiredAccess { get; set; }

            public UserAccess(Access Access = Access.Anonymous) : base()
            {
                RequiredAccess = Access;
            }

            protected override bool AuthorizeCore(HttpContextBase httpContext)
            {
                // On détecte si c'est une requête AJAX (pour ne pas rediriger brutalement en JS)
                bool ajaxRequest = httpContext.Request.IsAjaxRequest();

                try
                {
                    // 1. Personne n'est connecté
                    if (User.ConnectedUser == null)
                    {
                        if (!ajaxRequest)
                            httpContext.Response.Redirect("/Accounts/Login?message=Accès non autorisé!&success=false");
                        return false;
                    }

                    // 2. Utilisateur connecté mais n'a pas les droits OU est bloqué
                    if (User.ConnectedUser.Access < RequiredAccess || User.ConnectedUser.Blocked)
                    {
                        if (!ajaxRequest)
                        {
                            // On force la déconnexion avec le message "Accès illégal!"
                            httpContext.Response.Redirect("/Accounts/Logout?message=Accès illégal!");
                        }
                        return false;
                    }

                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }
    }
}
