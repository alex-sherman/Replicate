using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Replicate.Serialization;
using Replicate.Web;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReplicateWebTest
{
    public static class BaseTest
    {
        public static ControllerContext SetContext(ReplicateController controller, string body)
        {
            RouteData routeData = new RouteData();
            HttpContext httpContextMock = new DefaultHttpContext();
            var bytes = Encoding.UTF8.GetBytes(body);
            (httpContextMock.Request.Body = new MemoryStream()).Write(bytes, 0, bytes.Length);
            httpContextMock.Request.Body.Position = 0;
            return controller.ControllerContext = new ControllerContext()
            {
                RouteData = routeData,
                HttpContext = httpContextMock,
            };
        }
        public static async Task<string> Post(ReplicateController controller, string url, string body)
        {
            SetContext(controller, body);
            return ((ContentResult)(await controller.Post(url))).Content;
        }
        public static async Task<T> Post<T, U>(ReplicateController controller, string url, U request)
        {
            var serializer = controller.Serializer;
            return serializer.Deserialize<T>(await Post(controller, url, serializer.Serialize(request).ReadAllString()));
        }
        public static async Task<ActionResult> PostRaw<U>(ReplicateController controller, string url, U request)
        {
            var serializer = controller.Serializer;
            SetContext(controller, serializer.Serialize(request).ReadAllString());
            return await controller.Post(url);
        }
    }
}
