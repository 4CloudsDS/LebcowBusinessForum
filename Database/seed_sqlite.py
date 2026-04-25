import sqlite3
import uuid
from datetime import datetime

DB_PATH = 'lebcow_dev.db'

def seed():
    conn = sqlite3.connect(DB_PATH)
    cursor = conn.cursor()

    try:
        # 1. Roles
        admin_role_id = str(uuid.uuid4())
        owner_role_id = str(uuid.uuid4())
        user_role_id = str(uuid.uuid4())

        cursor.execute("INSERT OR IGNORE INTO Roles (Id, Name, NormalizedName, ConcurrencyStamp) VALUES (?, ?, ?, ?)", 
                       (admin_role_id, 'Admin', 'ADMIN', str(uuid.uuid4())))
        cursor.execute("INSERT OR IGNORE INTO Roles (Id, Name, NormalizedName, ConcurrencyStamp) VALUES (?, ?, ?, ?)", 
                       (owner_role_id, 'BusinessOwner', 'BUSINESSOWNER', str(uuid.uuid4())))
        cursor.execute("INSERT OR IGNORE INTO Roles (Id, Name, NormalizedName, ConcurrencyStamp) VALUES (?, ?, ?, ?)", 
                       (user_role_id, 'User', 'USER', str(uuid.uuid4())))

        # Get the actual ID for Admin if it already existed
        cursor.execute("SELECT Id FROM Roles WHERE NormalizedName = 'ADMIN'")
        admin_role_id = cursor.fetchone()[0]

        # 2. Admin User: Reabetswe Mogoswane
        admin_user_id = str(uuid.uuid4())
        admin_email = 'rmtjonko@gmail.com'
        
        cursor.execute("SELECT Id FROM Users WHERE Email = ?", (admin_email,))
        existing_user = cursor.fetchone()
        
        if not existing_user:
            cursor.execute("""
                INSERT INTO Users (
                    Id, FullName, CreatedAt, IsActive, UserName, NormalizedUserName, 
                    Email, NormalizedEmail, EmailConfirmed, PasswordHash, SecurityStamp, 
                    ConcurrencyStamp, PhoneNumber, PhoneNumberConfirmed, TwoFactorEnabled, 
                    LockoutEnd, LockoutEnabled, AccessFailedCount
                ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
            """, (
                admin_user_id, 'Reabetswe Mogoswane', datetime.utcnow().isoformat(), 1, 
                admin_email, admin_email.upper(), admin_email, admin_email.upper(), 
                1, 'AQAAAAEAACcQAAAAEPvX...', str(uuid.uuid4()), str(uuid.uuid4()), 
                '078 494 6161', 1, 0, None, 1, 0
            ))
            # Link to Admin Role
            cursor.execute("INSERT OR IGNORE INTO UserRoles (UserId, RoleId) VALUES (?, ?)", (admin_user_id, admin_role_id))
        else:
            admin_user_id = existing_user[0]

        # 3. Categories
        categories = [
            'Retail & Shopping', 'Food & Restaurants', 'Professional Services', 
            'Health & Wellness', 'Education & Training', 'Automotive', 
            'Construction & Trades', 'Technology & IT', 'Events & Entertainment', 
            'Agriculture & Farming'
        ]
        
        cat_map = {}
        for cat_name in categories:
            cursor.execute("SELECT CategoryId FROM Categories WHERE Name = ?", (cat_name,))
            existing_cat = cursor.fetchone()
            if not existing_cat:
                cat_id = str(uuid.uuid4())
                cursor.execute("INSERT INTO Categories (CategoryId, Name, ParentCategoryId) VALUES (?, ?, ?)", 
                               (cat_id, cat_name, None))
                cat_map[cat_name] = cat_id
            else:
                cat_map[cat_name] = existing_cat[0]

        # 4. Additional Users
        users_to_seed = [
            ('Thabo Mbeki', 'thabo@lebcow.co.za'),
            ('Lerato Moloi', 'lerato@agency.com'),
            ('Kgotso Masilo', 'kgotso@build.co.za'),
            ('Zanele Khumalo', 'zanele@wellness.com')
        ]
        user_ids = []
        for name, email in users_to_seed:
            u_id = str(uuid.uuid4())
            cursor.execute("SELECT Id FROM Users WHERE Email = ?", (email,))
            existing = cursor.fetchone()
            if not existing:
                cursor.execute("""
                    INSERT INTO Users (Id, FullName, CreatedAt, IsActive, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumber, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnd, LockoutEnabled, AccessFailedCount)
                    VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
                """, (u_id, name, datetime.utcnow().isoformat(), 1, email, email.upper(), email, email.upper(), 1, 'AQAAAAEAACcQAAAAEPvX...', str(uuid.uuid4()), str(uuid.uuid4()), '012 345 6789', 1, 0, None, 1, 0))
                user_ids.append(u_id)
            else:
                user_ids.append(existing[0])

        # 5. Businesses (Expanded)
        businesses_to_seed = [
            # IT Business for Admin
            {
                'Name': 'RM Tech Solutions',
                'CategoryId': cat_map['Technology & IT'],
                'Address': '123 Digital Drive, Sandton, 2196',
                'Phone': '078 494 6161',
                'Email': 'info@rmtech.io',
                'Website': 'https://marumanemogoswane-c9bfemhhggg3brdh.southafricanorth-01.azurewebsites.net/',
                'Description': 'Premium IT consultancy and software development specializing in AI-driven automation and cloud architecture.',
                'Status': 'active',
                'Region': 'Gauteng',
                'OwnerId': admin_user_id
            },
            {
                'Name': 'Lebowakgomo Mall Grocers',
                'CategoryId': cat_map['Retail & Shopping'],
                'Address': 'Zone F, Lebowakgomo, 0737',
                'Phone': '015 633 1000',
                'Email': 'contact@lebcow-grocers.co.za',
                'Website': None,
                'Description': 'Large retail store providing essential goods to the local community.',
                'Status': 'active',
                'Region': 'Limpopo',
                'OwnerId': None
            },
            {
                'Name': 'Village Kitchen',
                'CategoryId': cat_map['Food & Restaurants'],
                'Address': 'Plot 12, Ha-Makhuvha, 0950',
                'Phone': '015 962 1234',
                'Email': 'orders@villagekitchen.com',
                'Website': 'https://villagekitchen.com',
                'Description': 'Authentic Limpopo cuisine served daily.',
                'Status': 'active',
                'Region': 'Limpopo',
                'OwnerId': None
            },
            {
                'Name': 'Lekgotla Law Firm',
                'CategoryId': cat_map['Professional Services'],
                'Address': '45 West St, Cape Town, 8000',
                'Phone': '021 424 5566',
                'Email': 'service@lekgotlalaw.co.za',
                'Website': 'https://lekgotlalaw.co.za',
                'Description': 'Specializing in commercial and customary law.',
                'Status': 'active',
                'Region': 'Western Cape',
                'OwnerId': None
            },
            {
                'Name': 'Limpopo Wellness Center',
                'CategoryId': cat_map['Health & Wellness'],
                'Address': 'Shop 5, Polokwane Square, 0700',
                'Phone': '015 291 3000',
                'Email': 'info@limwellness.co.za',
                'Website': None,
                'Description': 'Holistic health services including physiotherapy and nutrition.',
                'Status': 'active',
                'Region': 'Limpopo',
                'OwnerId': None
            },
            {
                'Name': 'TechSkills Academy',
                'CategoryId': cat_map['Education & Training'],
                'Address': '88 Govan Mbeki Ave, Gqeberha, 6001',
                'Phone': '041 585 9900',
                'Email': 'enroll@techskills.ac.za',
                'Website': 'https://techskills.ac.za',
                'Description': 'Empowering youth with 4IR vocational skills.',
                'Status': 'active',
                'Region': 'Eastern Cape',
                'OwnerId': None
            },
            {
                'Name': 'Polokwane Auto Masters',
                'CategoryId': cat_map['Automotive'],
                'Address': 'Industrial Park, Polokwane, 0699',
                'Phone': '015 297 8888',
                'Email': 'service@plkautomasters.co.za',
                'Website': None,
                'Description': 'Full-service automotive repair and parts center.',
                'Status': 'active',
                'Region': 'Limpopo',
                'OwnerId': None
            },
            {
                'Name': 'IronOak Construction',
                'CategoryId': cat_map['Construction & Trades'],
                'Address': '52 Main Rd, Richards Bay, 3900',
                'Phone': '035 789 4455',
                'Email': 'build@ironoaka.com',
                'Website': 'https://ironoak.com',
                'Description': 'Civil engineering and residential development experts.',
                'Status': 'active',
                'Region': 'KwaZulu-Natal',
                'OwnerId': None
            },
            {
                'Name': 'Safari Elegance Events',
                'CategoryId': cat_map['Events & Entertainment'],
                'Address': 'Hoedspruit Wildlife Estate, 1380',
                'Phone': '015 793 0000',
                'Email': 'events@safarielegance.co.za',
                'Website': None,
                'Description': 'Curated event planning in the heart of the bushveld.',
                'Status': 'active',
                'Region': 'Mpumalanga',
                'OwnerId': None
            },
            {
                'Name': 'Sekhukhune Citrus Farms',
                'CategoryId': cat_map['Agriculture & Farming'],
                'Address': 'Tubatse District, 1150',
                'Phone': '013 231 7700',
                'Email': 'export@sekhukhune.farm',
                'Website': 'https://sekhukhune.farm',
                'Description': 'Supplier of export-quality citrus to global markets.',
                'Status': 'active',
                'Region': 'Limpopo',
                'OwnerId': None
            }
        ]

        for b in businesses_to_seed:
            cursor.execute("SELECT BusinessId FROM Businesses WHERE Name = ?", (b['Name'],))
            if not cursor.fetchone():
                b_id = str(uuid.uuid4())
                cursor.execute("""
                    INSERT INTO Businesses (
                        BusinessId, Name, CategoryId, Address, Phone, Email, 
                        Website, Description, LogoUrl, Status, Region, OwnerId, CreatedAt
                    ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
                """, (
                    b_id, b['Name'], b['CategoryId'], b['Address'], b['Phone'], 
                    b['Email'], b['Website'], b['Description'], None, 
                    b['Status'], b['Region'], b['OwnerId'], datetime.utcnow().isoformat()
                ))
                
                # 6. Listing for each business
                tier = 'premium' if b['Name'] == 'RM Tech Solutions' else 'free'
                cursor.execute("""
                    INSERT INTO Listings (
                        ListingId, BusinessId, Tier, StartDate, EndDate, PaymentStatus
                    ) VALUES (?, ?, ?, ?, ?, ?)
                """, (
                    str(uuid.uuid4()), b_id, tier, 
                    datetime.utcnow().isoformat(), 
                    datetime(2027, 4, 17).isoformat(), 'paid'
                ))

                # 7. Reviews for each business
                for i in range(2):
                    reviewer_id = user_ids[i % len(user_ids)]
                    cursor.execute("""
                        INSERT OR IGNORE INTO Reviews (ReviewId, BusinessId, UserId, Rating, Comment, CreatedAt)
                        VALUES (?, ?, ?, ?, ?, ?)
                    """, (str(uuid.uuid4()), b_id, reviewer_id, 4 + (i % 2), f"Great service from {b['Name']}!", datetime.utcnow().isoformat()))

                # 8. Favorites
                cursor.execute("INSERT OR IGNORE INTO UserFavorites (UserId, BusinessId, SavedAt) VALUES (?, ?, ?)",
                               (admin_user_id, b_id, datetime.utcnow().isoformat()))

        # 9. Events
        events = [
            ('Limpopo Tech Expo', 'Annual technology gathering for innovation.', '2026-06-15', 'Polokwane Hub'),
            ('Agri-Pulse Workshop', 'Modernizing citrus farming techniques.', '2026-07-10', 'Hoedspruit Center'),
            ('Retail Connect 2026', 'Networking for local shop owners.', '2026-08-05', 'Sandton Convention Center')
        ]
        for title, desc, date, loc in events:
            cursor.execute("""
                INSERT INTO Events (EventId, Title, Description, Date, Location, OrganizerId)
                VALUES (?, ?, ?, ?, ?, ?)
            """, (str(uuid.uuid4()), title, desc, date, loc, admin_user_id))

        # 10. Forum Posts
        posts = [
            ('Welcome to the Forum!', 'Let''s build a stronger business community together.', admin_user_id),
            ('IT Tips for Small Biz', 'Always keep your software updated!', admin_user_id),
            ('Funding Opportunities', 'Has anyone tried the new SME grant?', user_ids[0])
        ]
        for title, body, author in posts:
            cursor.execute("""
                INSERT INTO ForumPosts (PostId, Title, Body, AuthorId, CreatedAt)
                VALUES (?, ?, ?, ?, ?)
            """, (str(uuid.uuid4()), title, body, author, datetime.utcnow().isoformat()))

        # 11. Audit Logs
        cursor.execute("""
            INSERT INTO AuditLogs (LogId, UserId, Action, Timestamp)
            VALUES (?, ?, ?, ?)
        """, (str(uuid.uuid4()), admin_user_id, 'Database Seed Completed', datetime.utcnow().isoformat()))

        conn.commit()
        print("Successfully seeded database with admin user and legacy businesses.")
    except Exception as e:
        conn.rollback()
        print(f"Error seeding database: {e}")
    finally:
        conn.close()

if __name__ == "__main__":
    seed()
