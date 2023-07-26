using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace TN_Doc.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class DirEditorController : ControllerBase
    {
        private readonly ILogger<DirEditorController> _logger;

        public DirEditorController(ILogger<DirEditorController> logger) => _logger = logger;
        
        public async Task<IActionResult> GetAllDirectories()
        {
            return await Task.FromResult(Ok());
        }

        public async Task<IActionResult> UpdateUsers()
        {
            return await Task.FromResult(Ok());
        }
        
        public async Task<IActionResult> UpdateUserGroups()
        {
            return await Task.FromResult(Ok());
        }

        public async Task<IActionResult> UpdateLicences()
        {
            return await Task.FromResult(Ok());
        }

        
    }
}