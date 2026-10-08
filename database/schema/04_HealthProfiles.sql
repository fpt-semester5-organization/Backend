-- ============================================================
-- File: 04_HealthProfiles.sql
-- Bảng Thực Thể: HealthProfiles (3.3)
-- Mục đích nghiệp vụ: Hồ sơ thể trạng, chỉ số sinh học dinh dưỡng cá nhân hóa (BMR/TDEE/BMI).
-- ============================================================

CREATE TABLE dbo.HealthProfiles (
    User_ID INT NOT NULL,
    Dietary_School_ID INT NOT NULL,
    Gender VARCHAR(10) NOT NULL,
    Date_Of_Birth DATE NOT NULL,
    Height_Cm DECIMAL(5,1) NOT NULL,
    Current_Weight_Kg DECIMAL(5,1) NOT NULL,
    Target_Weight_Kg DECIMAL(5,1) NULL,
    BMI_Value DECIMAL(4,1) NOT NULL,
    BMR_Kcal DECIMAL(6,1) NOT NULL,
    TDEE_Kcal DECIMAL(6,1) NOT NULL,
    Target_Daily_Calories DECIMAL(6,1) NOT NULL,
    Requires_Allium_Free BIT NOT NULL DEFAULT 0,
    Region_Preference VARCHAR(20) NOT NULL DEFAULT 'SOUTH',
    Created_At DATETIME2(7) NOT NULL DEFAULT SYSDATETIME(),
    Updated_At DATETIME2(7) NOT NULL DEFAULT SYSDATETIME(),
    Is_Stale_Metrics BIT NOT NULL DEFAULT 0,
    HealthGoal VARCHAR(30) NOT NULL DEFAULT 'MAINTAIN',

    CONSTRAINT PK_HealthProfiles PRIMARY KEY CLUSTERED (User_ID),
    CONSTRAINT FK_HealthProfiles_Users FOREIGN KEY (User_ID) 
        REFERENCES dbo.Users(User_ID),
    CONSTRAINT CK_HealthProfiles_Gender CHECK (Gender IN ('MALE', 'FEMALE')),
    CONSTRAINT CK_HealthProfiles_Height CHECK (Height_Cm BETWEEN 50 AND 250),
    CONSTRAINT CK_HealthProfiles_Current_Weight CHECK (Current_Weight_Kg BETWEEN 20 AND 300),
    CONSTRAINT CK_HealthProfiles_HealthGoal CHECK (HealthGoal IN ('LOSE_WEIGHT', 'MAINTAIN', 'GAIN_MUSCLE'))
);

-- Khóa ngoại tới DietarySchools (bảng 01_DietarySchools)
IF OBJECT_ID('dbo.DietarySchools', 'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_HealthProfiles_DietarySchools')
    BEGIN
        ALTER TABLE dbo.HealthProfiles
        ADD CONSTRAINT FK_HealthProfiles_DietarySchools 
            FOREIGN KEY (Dietary_School_ID) REFERENCES dbo.DietarySchools(Dietary_School_ID);
    END
END;
