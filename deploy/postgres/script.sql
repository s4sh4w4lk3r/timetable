CREATE ROLE keycloak WITH
	LOGIN
	NOCREATEROLE
	INHERIT
	NOREPLICATION
	CONNECTION LIMIT -1
	PASSWORD 'keycloak';

CREATE DATABASE keycloak
    WITH
    OWNER = keycloak
    ENCODING = 'UTF8'
    LOCALE_PROVIDER = 'libc'
    CONNECTION LIMIT = -1
    IS_TEMPLATE = False;



CREATE ROLE aspnet WITH
	LOGIN
	NOCREATEROLE
	INHERIT
	NOREPLICATION
	CONNECTION LIMIT -1
	PASSWORD 'aspnet';

CREATE DATABASE aspnet
    WITH
    OWNER = aspnet
    ENCODING = 'UTF8'
    LOCALE_PROVIDER = 'libc'
    CONNECTION LIMIT = -1
    IS_TEMPLATE = False;
