﻿using Repository.Entities.Timetable;
using static Models.Request.GroupModels;

namespace Mappers
{
    public static class GroupMapper
    {
        public static Group ToEntity(this GroupPut group)
        {
            return new()
            {
                Id = group.Id,
                Name = group.Name,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }
    }
}
