SEED VIA UPDATE-DATABASE
========================
1. Rebuild solution
2. Package Manager Console — Default project: MathematicsLessons.Infrastructure

   Add-Migration SeedCatalog
   Update-Database

3. This inserts (from CatalogSeed.cs via ApplicationDbContext):
   - Free + Math Enthusiast prices
   - 1h / 1.5h / 2h private lesson prices
   - 4 courses (Beginner → College)
   - All sections (topics) and lessons with sample video
   - 2 exercises per lesson

4. If Update-Database fails because data/IDs already exist:
   - Use a new database name in appsettings.json, OR
   - Drop the old database, then Update-Database again

5. Run the site → Courses + Subscription prices should appear for all users.
