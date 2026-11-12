Math Lessons — full package
===========================
1. Open MathLessons-Fixed\MathematicsLessons.sln
2. Rebuild Solution
3. In Package Manager Console (Default project: MathematicsLessons.Infrastructure):
   Add-Migration VideoFlagsAndCleanup
   Update-Database
4. Run the site, login as Admin (OwnerEmail in appsettings)
5. Admin panel -> Load demo courses (once, empty catalog only)

Features included:
- Light (white/blue) + Dark (gray/gold-ochre) theme toggle
- Bootstrap 4.6 CDN + custom site.css
- Skool-like course sections
- Video end -> Practice exercises -> pass unlocks next / fail rewatch
- Teacher/Admin upload + remove video
- Load demo courses via Admin button (no CatalogSeeder)
- Donation removed
