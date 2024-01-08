﻿using Repository.Entities.Timetable;

namespace Mappers
{
    public static class ActualTimetableMapper
    {
        public static ActualTimetable ToEntity(this Models.Request.ActualTimetableModels.ActualTimetablePut model)
        {
            return new()
            {
                Id = model.Id,
                GroupId = model.GroupId,
                WeekNumber = model.WeekNumber,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }
    }
}
