using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using WebAppSystems.Services;
using WebAppSystems.Models;

public class BasicAuthenticationFilterAttribute : ActionFilterAttribute
{
    
    private readonly AttorneyService _attorneyService;

    public BasicAuthenticationFilterAttribute(AttorneyService attorneyService)
    {
        _attorneyService = attorneyService;
    }
    public BasicAuthenticationFilterAttribute()
    {        
    }



    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (!IsUserAuthenticated(context.HttpContext))
        {
            context.Result = new UnauthorizedResult();
        }
    }

    public bool IsUserAuthenticated(HttpContext context)
    {
        string authHeader = context.Request.Headers["Authorization"];

        if (authHeader != null && authHeader.StartsWith("Basic "))
        {
            // Extract the credentials
            string encodedUsernamePassword = authHeader.Substring("Basic ".Length).Trim();
            Encoding encoding = Encoding.GetEncoding("iso-8859-1");
            string usernamePassword = encoding.GetString(Convert.FromBase64String(encodedUsernamePassword));
            int separatorIndex = usernamePassword.IndexOf(':');
            string username = usernamePassword.Substring(0, separatorIndex);
            string password = usernamePassword.Substring(separatorIndex + 1);            

            if (_attorneyService.IsValidUser(username, password))
            {
                return true;
            }
        }
        return false;
    }    

}
