﻿using Repository.Database;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Serialization;
using static Services.Asc.Timetable.StaticDeterminers;

namespace Services.Asc.Timetable
{
    internal class Converter(TimetableContext timetableContext)
    {
        public List<Repository.Entities.Timetable.Cards.Parts.Cabinet> Cabinets { get; private set; } = [];
        public List<Repository.Entities.Timetable.Cards.Parts.Teacher> Teachers { get; private set; } = [];
        public List<Repository.Entities.Timetable.Cards.Parts.LessonTime> LessonTimes { get; private set; } = [];
        public List<Repository.Entities.Timetable.Cards.Parts.Subject> Subjects { get; private set; } = [];
        public List<Repository.Entities.Timetable.Cards.StableCard> StableCards { get; private set; } = [];
        public List<Repository.Entities.Timetable.StableTimetable> StableTimetables { get; private set; } = [];
        public List<Repository.Entities.Timetable.Group> Groups { get; private set; } = [];

        private AscClasses.AscTimetable _ascTimetable = null!;

        public async Task ConvertAndSaveAsync(XmlReader xmlReader, CancellationToken cancellationToken = default)
        {
            var serializer = new XmlSerializer(typeof(AscClasses.AscTimetable));
            _ascTimetable = serializer.Deserialize(xmlReader) as AscClasses.AscTimetable ?? throw new SerializationException();

            xmlReader.Dispose();

            await StartConvertingAsync(cancellationToken);
        }

        public async Task ConvertAndSaveAsync(string path, CancellationToken cancellationToken = default)
        {
            var serializer = new XmlSerializer(typeof(AscClasses.AscTimetable));
            var xmlReader = XmlReader.Create(path);
            _ascTimetable = serializer.Deserialize(xmlReader) as AscClasses.AscTimetable ?? throw new SerializationException();

            xmlReader.Dispose();

            await StartConvertingAsync(cancellationToken);
        }

        private async Task StartConvertingAsync(CancellationToken cancellationToken = default)
        {
            FillDataForCards();
            await SaveDataForCardsAsync(cancellationToken);
            await CreateCardsAndSaveLocallyAsync(cancellationToken);
            await timetableContext.SaveChangesAsync(cancellationToken);
        }

        private void FillDataForCards()
        {
            ArgumentNullException.ThrowIfNull(_ascTimetable);

            Groups = _ascTimetable.Classes.Class.Select(e => new Repository.Entities.Timetable.Group()
            { Id = default, AscId = e.Id, Name = e.Name, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }).ToList();

            Teachers = _ascTimetable.Teachers.Teacher.Select(e => new Repository.Entities.Timetable.Cards.Parts.Teacher()
            {
                AscId = e.Id,
                Lastname = e.Lastname,
                Firstname = e.Firstname,
                Middlename = string.Empty,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }).ToList();

            Subjects = _ascTimetable.Subjects.Subject.Select(e => new Repository.Entities.Timetable.Cards.Parts.Subject()
            { Id = default, AscId = e.Id, Name = e.Name, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }).ToList();

            LessonTimes = _ascTimetable.Periods.Period.Select(e => new Repository.Entities.Timetable.Cards.Parts.LessonTime()
            { Id = default, StartsAt = TimeOnly.Parse(e.Starttime), EndsAt = TimeOnly.Parse(e.Endtime), Number = int.Parse(e._period), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }).ToList();

            Cabinets = _ascTimetable.Classrooms.Classroom.Select(e => new Repository.Entities.Timetable.Cards.Parts.Cabinet()
            {
                Id = default,
                AscId = e.Id,
                Number = e.Short,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Address = _ascTimetable.Buildings.Building.Single(b => b.Id == e.Buildingid).Name,
                FullName = e.Name
            }).ToList();
        }
        private async Task SaveDataForCardsAsync(CancellationToken cancellationToken = default)
        {
            //await timetableContext.AddRangeAsync([Groups, Teachers, Subjects, LessonTimes, Cabinets], cancellationToken);
            await timetableContext.Groups.AddRangeAsync(Groups, cancellationToken);
            await timetableContext.Teachers.AddRangeAsync(Teachers, cancellationToken);
            await timetableContext.Subjects.AddRangeAsync(Subjects, cancellationToken);
            await timetableContext.LessonTimes.AddRangeAsync(LessonTimes, cancellationToken);
            await timetableContext.Cabinets.AddRangeAsync(Cabinets, cancellationToken);
            await timetableContext.SaveChangesAsync(cancellationToken);

        }
        private async Task CreateCardsAndSaveLocallyAsync(CancellationToken cancellationToken = default)
        {
            List<Repository.Entities.Timetable.StableTimetable> stableTimetables = [];

            foreach (var group in Groups)
            {
                var stableCardsOfCurrentGroup = new List<Repository.Entities.Timetable.Cards.StableCard>();
                var lessonsOfCurrentGroup = _ascTimetable.Lessons.Lesson.Where(e => e.Classids == group.AscId).ToList();
                var ascCardsOfCurrentGroup = new List<AscClasses.Card>();
                foreach (var lesson in lessonsOfCurrentGroup)
                {
                    foreach (var card in _ascTimetable.Cards.Card)
                    {
                        if (card.Lessonid == lesson.Id)
                        {
                            ascCardsOfCurrentGroup.Add(card);
                        }
                    }
                }

                foreach (var card in ascCardsOfCurrentGroup)
                {
                    var lesson = _ascTimetable.Lessons.Lesson.Single(e => e.Id == card.Lessonid);

                    DayOfWeek dayOfWeek = DetermineDayOfWeek(card.Days);

                    Repository.Entities.Timetable.Cards.Parts.Teacher teacher = Teachers.Single(e => e.AscId == lesson.Teacherids);
                    Repository.Entities.Timetable.Cards.Parts.Subject subject = Subjects.Single(e => e.AscId == lesson.Subjectid);
                    Repository.Entities.Timetable.Cards.Parts.LessonTime lessonTime = LessonTimes.Single(e => e.Number == int.Parse(card.Period));
                    Repository.Entities.Timetable.Cards.Parts.Cabinet cabinet = DetermineCabinets(Cabinets, lesson.Classroomids);

                    Repository.Entities.Timetable.Cards.Parts.SubGroup subGroup = DetermineSubgroup(_ascTimetable.Groups.Group.Single(e => e.Id == lesson.Groupids).Name);
                    switch (DetermineWeekEvenness(card.Weeks))
                    {
                        case WeekEvenness.Both:
                            stableCardsOfCurrentGroup.Add(new Repository.Entities.Timetable.Cards.StableCard()
                            {
                                Id = default,
                                SubjectId = subject.Id,
                                LessonTimeId = lessonTime.Id,
                                TeacherId = teacher.Id,
                                CabinetId = cabinet.Id,
                                SubGroup = subGroup,
                                DayOfWeek = dayOfWeek,
                                IsWeekEven = true,
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow,
                                RelatedTimetableId = default
                            });

                            stableCardsOfCurrentGroup.Add(new Repository.Entities.Timetable.Cards.StableCard()
                            {
                                Id = default,
                                SubjectId = subject.Id,
                                LessonTimeId = lessonTime.Id,
                                TeacherId = teacher.Id,
                                CabinetId = cabinet.Id,
                                SubGroup = subGroup,
                                DayOfWeek = dayOfWeek,
                                IsWeekEven = false,
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow,
                                RelatedTimetableId = default
                            });
                            break;

                        case WeekEvenness.Even:
                            stableCardsOfCurrentGroup.Add(new Repository.Entities.Timetable.Cards.StableCard()
                            {
                                Id = default,
                                SubjectId = subject.Id,
                                LessonTimeId = lessonTime.Id,
                                TeacherId = teacher.Id,
                                CabinetId = cabinet.Id,
                                SubGroup = subGroup,
                                DayOfWeek = dayOfWeek,
                                IsWeekEven = true,
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow,
                                RelatedTimetableId = default
                            });
                            break;

                        case WeekEvenness.Odd:
                            stableCardsOfCurrentGroup.Add(new Repository.Entities.Timetable.Cards.StableCard()
                            {
                                Id = default,
                                SubjectId = subject.Id,
                                LessonTimeId = lessonTime.Id,
                                TeacherId = teacher.Id,
                                CabinetId = cabinet.Id,
                                SubGroup = subGroup,
                                DayOfWeek = dayOfWeek,
                                IsWeekEven = false,
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow,
                                RelatedTimetableId = default
                            });
                            break;

                        default:
                            throw new ArgumentException("Четностb недели не определена.");
                    }

                }
                var currentStableTimetable = new Repository.Entities.Timetable.StableTimetable() { Id = default, Cards = stableCardsOfCurrentGroup, GroupId = group.Id, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
                await timetableContext.AddAsync(currentStableTimetable, cancellationToken);
            }
        }
    }
}
