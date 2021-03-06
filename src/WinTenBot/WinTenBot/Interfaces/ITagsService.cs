﻿using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using WinTenBot.Model;

namespace WinTenBot.Interfaces
{
    public interface ITagsService
    {
        Task<List<CloudTag>> GetTagsAsync();
    }
}