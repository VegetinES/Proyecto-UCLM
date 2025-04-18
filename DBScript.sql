-- Eliminar tablas si existen (en orden inverso a la creación para evitar problemas con las claves foráneas)
DROP TABLE IF EXISTS LEVEL_ATTEMPS;
DROP TABLE IF EXISTS STADISTICS;
DROP TABLE IF EXISTS PARENTAL_CONTROL;
DROP TABLE IF EXISTS CONFIGURATION;
DROP TABLE IF EXISTS USERS_LOGIN;
DROP TABLE IF EXISTS PROFILES;
DROP TABLE IF EXISTS NO_PROFILE;
DROP TABLE IF EXISTS UNREGISTERED_USER;
DROP TABLE IF EXISTS REGISTERED_USER;
DROP TABLE IF EXISTS USERS;

-- Tabla principal de usuarios
CREATE TABLE USERS (
    UID VARCHAR(50) PRIMARY KEY
    -- Al principio será 0 para usuarios no registrados
);

-- Tabla para usuarios registrados
CREATE TABLE REGISTERED_USER (
    UID VARCHAR(50) PRIMARY KEY,
    last_login DATETIME,
    creation_date DATETIME,
    tutor BOOLEAN NOT NULL,
    FOREIGN KEY (UID) REFERENCES USERS(UID) ON DELETE CASCADE
);

-- Tabla para usuarios no registrados
CREATE TABLE UNREGISTERED_USER (
    UID VARCHAR(50) PRIMARY KEY,
    FOREIGN KEY (UID) REFERENCES USERS(UID) ON DELETE CASCADE
);

-- Tabla para registro de inicios de sesión
CREATE TABLE USERS_LOGIN (
    ID BIGINT AUTO_INCREMENT PRIMARY KEY,
    UID VARCHAR(50) NOT NULL,
    login_date DATETIME NOT NULL,
    FOREIGN KEY (UID) REFERENCES REGISTERED_USER(UID) ON DELETE CASCADE
);

-- Tabla para perfiles de usuarios registrados
CREATE TABLE PROFILES (
    PROFILEID INT AUTO_INCREMENT PRIMARY KEY,
    UID VARCHAR(50) NOT NULL,
    name VARCHAR(50) NOT NULL,
    gender VARCHAR(10) CHECK (gender IN ('CHICO', 'CHICA')),
    FOREIGN KEY (UID) REFERENCES REGISTERED_USER(UID) ON DELETE CASCADE
);

-- Tabla para usuarios registrados sin perfil
CREATE TABLE NO_PROFILE (
    UID VARCHAR(50) PRIMARY KEY,
    FOREIGN KEY (UID) REFERENCES REGISTERED_USER(UID) ON DELETE CASCADE
);

-- Tabla para configuración de usuarios
CREATE TABLE CONFIGURATION (
    USERID VARCHAR(50) PRIMARY KEY,
    colors INT(1) DEFAULT 1 CHECK (colors BETWEEN 1 AND 5),
    auto_narrator BOOLEAN DEFAULT TRUE,
    FOREIGN KEY (USERID) REFERENCES USERS(UID) ON DELETE CASCADE
);

-- Tabla para control parental
CREATE TABLE PARENTAL_CONTROL (
    USERID VARCHAR(50) PRIMARY KEY,
    activated BOOLEAN DEFAULT FALSE,
    pin VARCHAR(100), -- Hash del PIN de 4 dígitos
    sound_conf BOOLEAN DEFAULT TRUE,
    accesibility_conf BOOLEAN DEFAULT TRUE,
    stadistics_conf BOOLEAN DEFAULT TRUE,
    about_conf BOOLEAN DEFAULT TRUE,
    profile BOOLEAN DEFAULT TRUE,
    FOREIGN KEY (USERID) REFERENCES USERS(UID) ON DELETE CASCADE
);

-- Tabla para estadísticas de juego
CREATE TABLE STADISTICS (
    STATSID BIGINT AUTO_INCREMENT PRIMARY KEY,
    USERID VARCHAR(50) NOT NULL,
    level INT(1) DEFAULT 1,
    completed INT(2) DEFAULT 0,
    failed INT(2) DEFAULT 0,
    total INT(3) DEFAULT 0,
    best_time INT(4),
    FOREIGN KEY (USERID) REFERENCES USERS(UID) ON DELETE CASCADE
);

-- Tabla para intentos de nivel
CREATE TABLE LEVEL_ATTEMPS (
    ATTEMPID INT AUTO_INCREMENT PRIMARY KEY,
    STATSID BIGINT NOT NULL,
    completed BOOLEAN DEFAULT FALSE,
    help BOOLEAN DEFAULT FALSE,
    time INT(4),
    completion_date DATETIME,
    FOREIGN KEY (STATSID) REFERENCES STADISTICS(STATSID) ON DELETE CASCADE
);