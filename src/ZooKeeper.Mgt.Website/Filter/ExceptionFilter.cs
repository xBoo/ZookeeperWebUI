using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using ZooKeeper.Mgt.Website.Common;

namespace ZooKeeper.Mgt.Website.Filter
{
    public class ExceptionFilter : ExceptionFilterAttribute
    {
        readonly ILogger _logger;

        public ExceptionFilter(ILogger<ExceptionFilter> logger)
        {
            _logger = logger;
        }

        public override void OnException(ExceptionContext context)
        {
            var result = new BizResult<bool>(EnumSystemCode.Exception, context.Exception.Message) { BusinessCode = -1, BusinessMessage = context.Exception.Message };
            this._logger.LogError(context.Exception, context.Exception.Message);
            context.Result = new JsonResult(result);
        }
    }
}
