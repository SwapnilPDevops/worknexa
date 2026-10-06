-- Create the application database from the postgres maintenance database.
-- Tables are created by Entity Framework in schema worknexa.
SELECT 'CREATE DATABASE worknexa_db'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'worknexa_db')\gexec
