using MathematicsLessons.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace MathematicsLessons.Infrastructure.Data;

/// <summary>
/// Catalog + prices seed for Update-Database (HasData).
/// Every lesson gets a demo video; URLs rotate so sections look different.
/// </summary>
public static class CatalogSeed
{
    public static readonly string[] DemoVideos =
    {
        "https://www.w3schools.com/html/mov_bbb.mp4",
        "https://www.w3schools.com/html/movie.mp4",
        "https://interactive-examples.mdn.mozilla.net/media/cc0-videos/flower.mp4",
        "https://filesamples.com/samples/video/mp4/sample_640x360.mp4",
        "https://download.samplelib.com/mp4/sample-5s.mp4",
        "https://download.samplelib.com/mp4/sample-10s.mp4",
        "https://download.samplelib.com/mp4/sample-15s.mp4",
        "https://download.samplelib.com/mp4/sample-20s.mp4",
    };

    private static string VideoFor(int lessonId) =>
        DemoVideos[(lessonId - 1) % DemoVideos.Length];

    public static void Apply(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Sub>().HasData(
            new Sub
            {
                Id = 1,
                Type = "Free",
                PricePerMonth = 0,
                PricePerYear = 0,
                Description = "First Beginner lesson free",
                IncludesPrivateSessions = false,
                MaxSessionsPerMonth = 0
            },
            new Sub
            {
                Id = 2,
                Type = "Math Enthusiast",
                PricePerMonth = 19.99m,
                PricePerYear = 199m,
                Description = "Full access to all courses and private lessons",
                IncludesPrivateSessions = true,
                MaxSessionsPerMonth = 8
            });

        modelBuilder.Entity<AppointmentPlan>().HasData(
            new AppointmentPlan { Id = 1, Name = "1 hour", DurationMinutes = 60, Price = 25m, Description = "Private lesson 60 minutes" },
            new AppointmentPlan { Id = 2, Name = "1.5 hours", DurationMinutes = 90, Price = 35m, Description = "Private lesson 90 minutes" },
            new AppointmentPlan { Id = 3, Name = "2 hours", DurationMinutes = 120, Price = 45m, Description = "Private lesson 120 minutes" });

        modelBuilder.Entity<Level>().HasData(
            new Level { Id = 1, Name = "Beginner", GradeRange = "1 – 4 клас", SortOrder = 1, Description = "Основи за 1–4 клас: числа, смятане, фигури." },
            new Level { Id = 2, Name = "Intermediate", GradeRange = "5 – 7 клас", SortOrder = 2, Description = "Дроби, уравнения, геометрия за 5–7 клас." },
            new Level { Id = 3, Name = "Advanced", GradeRange = "8 – 12 клас", SortOrder = 3, Description = "Алгебра, функции, стереометрия към матура." },
            new Level { Id = 4, Name = "College", GradeRange = "Висша математика част 1–3", SortOrder = 4, Description = "Анализ, линейна алгебра, вероятности." });

        var topics = new List<Topic>();
        var lessons = new List<Lesson>();
        var exercises = new List<Exercise>();
        int tid = 1, lid = 1, eid = 1;

        void Topic(int levelId, string name, int sort, params string[] titles)
        {
            var t = new Topic
            {
                Id = tid++,
                LevelId = levelId,
                Name = name,
                Description = name,
                SortOrder = sort
            };
            topics.Add(t);
            int order = sort * 10;
            foreach (var title in titles)
            {
                var lessonId = lid++;
                var lesson = new Lesson
                {
                    Id = lessonId,
                    LevelId = levelId,
                    TopicId = t.Id,
                    Title = title,
                    Content =
                        $"Урок: {title}.\n\n" +
                        "1) Гледай видеото докрай.\n" +
                        "2) Прочети кратките бележки.\n" +
                        "3) Реши упражненията под видеото — при грешка гледай урока отново.\n" +
                        "4) След успех се отключва следващият урок в секцията.",
                    Video = VideoFor(lessonId),
                    SortOrder = order++,
                    DurationMinutes = 15,
                    PointsReward = 25
                };
                lessons.Add(lesson);
                var key = title.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "ok";
                exercises.Add(new Exercise
                {
                    Id = eid++,
                    LessonId = lesson.Id,
                    Question = $"Какво е основната тема на „{title}“? (ключова дума)",
                    Solution = key,
                    Hint = "Виж заглавието на урока.",
                    PointsReward = 10
                });
                exercises.Add(new Exercise
                {
                    Id = eid++,
                    LessonId = lesson.Id,
                    Question = "Готов ли си за следващия урок? (yes / да)",
                    Solution = "yes",
                    Hint = "Напиши yes или да",
                    PointsReward = 5
                });
            }
        }

        // Beginner
        Topic(1, "1 клас — Числа и броене", 11, "Числа до 10", "Сравняване на числа", "Числова ос");
        Topic(1, "1 клас — Събиране и изваждане", 12, "Събиране до 10", "Изваждане до 10", "Задачи с текст");
        Topic(1, "1 клас — Фигури", 13, "Квадрат и кръг", "Триъгълник", "Разпознаване на фигури");
        Topic(1, "2 клас — Числа до 100", 21, "Десетици и единици", "Събиране с преминаване", "Изваждане с преминаване");
        Topic(1, "2 клас — Умножение", 22, "Какво е умножение", "Таблица 2 и 5", "Прости задачи");
        Topic(1, "2 клас — Фигури и измерване", 23, "Отсечка", "Сантиметър", "Правоъгълник");
        Topic(1, "3 клас — Числа до 1000", 31, "Разрядна форма", "Събиране на трицифрени", "Изваждане на трицифрени");
        Topic(1, "3 клас — Умножение и деление", 32, "Таблица до 10", "Деление — идея", "Задачи от живота");
        Topic(1, "3 клас — Геометрия", 33, "Периметър", "Видове триъгълници", "Симетрия — начало");
        Topic(1, "4 клас — Големи числа", 41, "Числа над 1000", "Закони на смятането", "Оценка на резултат");
        Topic(1, "4 клас — Дроби", 42, "Какво е дроб", "Единични дроби", "Сравняване на дроби");
        Topic(1, "4 клас — Лице", 43, "Лице на правоъгълник", "Единици за лице", "Задачи с чертеж");

        // Intermediate
        Topic(2, "5 клас — Дроби и десетични", 51, "Събиране на дроби", "Десетични числа", "Преобразуване");
        Topic(2, "5 клас — Уравнения", 52, "Неизвестно число", "Прости уравнения", "Проверка");
        Topic(2, "5 клас — Геометрия", 53, "Ъгли", "Триъгълник — свойства", "Лице на триъгълник");
        Topic(2, "6 клас — Проценти", 61, "Отношение", "Пропорция", "Проценти в задачи");
        Topic(2, "6 клас — Алгебра", 62, "Изрази с букви", "Опростяване", "Линейни уравнения");
        Topic(2, "6 клас — Геометрия", 63, "Окръжност", "Лице на кръг — идея", "Координатна система");
        Topic(2, "7 клас — Рационални числа", 71, "Отрицателни числа", "Смятане с рационални", "Числова ос");
        Topic(2, "7 клас — Уравнения", 72, "Линейни уравнения", "Неравенства", "Текстови задачи");
        Topic(2, "7 клас — Геометрия", 73, "Питагорова теорема — увод", "Подобни фигури", "Лица на многоъгълници");

        // Advanced
        Topic(3, "8 клас — Алгебра", 81, "Степенуване", "Многочлени", "Съкратени формули");
        Topic(3, "8 клас — Функции", 82, "Какво е функция", "Линейна функция", "Графика");
        Topic(3, "8 клас — Геометрия", 83, "Вектори — идея", "Окръжност и ъгли", "Лица и периметри");
        Topic(3, "9 клас — Квадратни уравнения", 91, "Дискриминанта", "Корени", "Текстови задачи");
        Topic(3, "9 клас — Функции", 92, "Квадратна функция", "Графики", "Трансформации");
        Topic(3, "9 клас — Планиметрия", 93, "Подобие", "Тригонометрия в правоъгълен", "Лица");
        Topic(3, "10 клас — Логаритми", 101, "Показателни уравнения", "Логаритми", "Свойства");
        Topic(3, "10 клас — Тригонометрия", 102, "sin cos tan", "Основни тъждества", "Уравнения");
        Topic(3, "10 клас — Стереометрия", 103, "Призма", "Пирамида", "Обем и лице");
        Topic(3, "11 клас — Комбинаторика", 111, "Пермутации", "Комбинации", "Задачи");
        Topic(3, "11 клас — Вероятности", 112, "Класическа вероятност", "Събития", "Практически задачи");
        Topic(3, "11 клас — Стереометрия", 113, "Цилиндър", "Конус", "Сфера");
        Topic(3, "12 клас — Матура", 121, "Алгебра — преговор", "Функции — преговор", "Геометрия — преговор");
        Topic(3, "12 клас — Параметър", 122, "Уравнения с параметър", "Неравенства с параметър", "Графичен подход");
        Topic(3, "12 клас — Смесени", 123, "Смесени задачи 1", "Смесени задачи 2", "Пробен вариант");

        // College
        Topic(4, "Part 1 — Граници", 201, "Граница на редица", "Граница на функция", "Непрекъснатост");
        Topic(4, "Part 1 — Производни", 202, "Дефиниция на производна", "Правила за диференциране", "Приложения");
        Topic(4, "Part 2 — Матрици", 203, "Операции с матрици", "Детерминанти", "Обратна матрица");
        Topic(4, "Part 2 — Вектори", 204, "Линейна независимост", "Базис", "Линейни изображения");
        Topic(4, "Part 3 — Вероятности", 205, "Дискретни величини", "Очакване и дисперсия", "Нормално разпределение");
        Topic(4, "Part 3 — Статистика", 206, "Описателна статистика", "Извадки", "Интервали — увод");

        modelBuilder.Entity<Topic>().HasData(topics);
        modelBuilder.Entity<Lesson>().HasData(lessons);
        modelBuilder.Entity<Exercise>().HasData(exercises);
    }
}
