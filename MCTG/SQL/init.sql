-- Create users table
    CREATE TABLE IF NOT EXISTS users (
    id SERIAL PRIMARY KEY,
    username VARCHAR(255) UNIQUE NOT NULL,
    password VARCHAR(255) NOT NULL,
    coins INTEGER NOT NULL DEFAULT 20,
    elo INTEGER NOT NULL DEFAULT 100
    );

-- Create cards table
    CREATE TABLE IF NOT EXISTS cards (
    id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    damage INTEGER NOT NULL,
    element_type VARCHAR(50) NOT NULL,
    card_type VARCHAR(50) NOT NULL,
    user_id INTEGER,
    in_deck BOOLEAN DEFAULT false,
    FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
    );

-- Create battle_history table
    CREATE TABLE IF NOT EXISTS battle_history (
    id SERIAL PRIMARY KEY,
    player1_id INTEGER REFERENCES users(id),
    player2_id INTEGER REFERENCES users(id),
    winner_id INTEGER REFERENCES users(id),
    battle_log TEXT NOT NULL,
    player1_elo_change INTEGER NOT NULL,
    player2_elo_change INTEGER NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
    );

-- Create user_stats table
    CREATE TABLE IF NOT EXISTS user_stats (
    id SERIAL PRIMARY KEY,
    user_id INTEGER REFERENCES users(id),
    games_played INTEGER DEFAULT 0,
    wins INTEGER DEFAULT 0,
    losses INTEGER DEFAULT 0,
    draws INTEGER DEFAULT 0,
    elo INTEGER DEFAULT 100
    );

-- Create user_profiles table
    CREATE TABLE IF NOT EXISTS user_profiles (
    user_id INTEGER PRIMARY KEY REFERENCES users(id),
    name VARCHAR(255),
    bio TEXT,
    image VARCHAR(255)
    );

-- Create trades table
    CREATE TABLE IF NOT EXISTS trades (
    id SERIAL PRIMARY KEY,
    card_id INTEGER REFERENCES cards(id) ON DELETE CASCADE,
    user_id INTEGER REFERENCES users(id) ON DELETE CASCADE,
    required_type VARCHAR(50) NOT NULL,
    minimum_damage INTEGER NOT NULL
    );

-- Create packages table
    CREATE TABLE IF NOT EXISTS packages (
    id SERIAL PRIMARY KEY,
    is_sold BOOLEAN DEFAULT false,
    bought_by_user_id INTEGER REFERENCES users(id),
    purchase_date TIMESTAMP
    );

-- Create package_cards table
    CREATE TABLE IF NOT EXISTS package_cards (
    package_id INTEGER REFERENCES packages(id) ON DELETE CASCADE,
    card_id INTEGER REFERENCES cards(id) ON DELETE CASCADE,
    PRIMARY KEY (package_id, card_id)
    );
