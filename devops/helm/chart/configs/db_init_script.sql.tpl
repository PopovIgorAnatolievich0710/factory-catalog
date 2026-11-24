DO $$
BEGIN
  CREATE USER "migrations_user" with password '${db_password_migrations}';
  EXCEPTION WHEN duplicate_object THEN
  RAISE NOTICE 'user already exists';
END
$$;
DO $$
BEGIN
  grant rds_superuser to "migrations_user";
  GRANT CREATE ON DATABASE ${database_name} TO migrations_user;
  EXCEPTION WHEN others THEN
  RAISE NOTICE 'not running in AWS(RDS)';
  ALTER USER "migrations_user" WITH SUPERUSER;
END
$$;
