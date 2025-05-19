using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LLMCommService.Models;

namespace LLMCommService.Interfaces
{
    public interface IClaudeService
    {
        Task<string> ExtractMessageAsync(string userMessage);
    }
}