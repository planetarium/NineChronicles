using Microsoft.AspNetCore.Mvc;

namespace NineChronicles.Standalone
{
    public class ApiController : Controller
    {
        // [HttpPost("/graphql/")]
        // public IActionResult GetGraphQLResult(
        //     [FromBody] GraphQLBody body
        // )
        // var json = _schema.Execute(_ =>
        //     {
        //         _.UserContext = _context;
        //         _.Query = body.Query;
        //         _.ThrowOnUnhandledException = true;
        //         if (body.Variables != null)
        //         {
        //             _.Inputs = body.Variables.ToString(Newtonsoft.Json.Formatting.None).ToInputs();
        //         }
        //     });
        //     return Ok(JObject.Parse(json));
    }
}
