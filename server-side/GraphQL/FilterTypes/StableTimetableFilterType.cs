﻿using HotChocolate.Data.Filters;
using Repository.Entities.Timetable;

namespace GraphQL.FilterTypes
{
    internal class StableTimetableFilterType : FilterInputType<StableTimetable>
    {
        protected override void Configure(IFilterInputTypeDescriptor<StableTimetable> descriptor)
        {
            descriptor.BindFieldsImplicitly();
            descriptor.Field(e => e.Cards).Type<StableCardFilterType>();
        }
    }
}
