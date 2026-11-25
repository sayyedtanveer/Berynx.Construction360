-- SaaS licensing foundation schema.
-- Prerequisites: P1-M01 onboarding and P1-M02 identity/auth schema are installed.
-- This script is DDL only; constraints and indexes are in 03-indexes-constraints.sql.

BEGIN;

CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

CREATE TABLE IF NOT EXISTS platform_license_audit (
    "TenantId" uuid NOT NULL,
    "EventId" uuid NOT NULL,
    "ActorIdentityId" uuid NOT NULL,
    "ActorEmail" character varying(256) NOT NULL,
    "Action" character varying(80) NOT NULL,
    "EntityType" character varying(80) NOT NULL,
    "EntityId" uuid NOT NULL,
    "Reason" character varying(1000) NOT NULL,
    "Before" jsonb NOT NULL,
    "After" jsonb NOT NULL,
    "Origin" jsonb NOT NULL,
    "At" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_platform_license_audit" PRIMARY KEY ("TenantId", "EventId")
);

CREATE TABLE IF NOT EXISTS platform_module (
    "ModuleCode" character varying(48) NOT NULL,
    "Name" character varying(120) NOT NULL,
    "Description" character varying(1000) NOT NULL,
    "IsCore" boolean NOT NULL,
    "IsActive" boolean NOT NULL,
    "SortOrder" integer NOT NULL,
    CONSTRAINT "PK_platform_module" PRIMARY KEY ("ModuleCode")
);

CREATE TABLE IF NOT EXISTS platform_plan (
    "PlanCode" character varying(48) NOT NULL,
    "Name" character varying(120) NOT NULL,
    "Description" character varying(1000) NOT NULL,
    "IsActive" boolean NOT NULL,
    "MaxUsers" bigint NOT NULL,
    "MaxStorageBytes" bigint NOT NULL,
    "MaxDocumentsPerMonth" bigint NOT NULL,
    "MaxMessagesPerMonth" bigint NOT NULL,
    "TrialDays" integer NOT NULL,
    "GracePeriodDays" integer NOT NULL,
    "PriceAmount" numeric(18,2),
    "Currency" character varying(3) NOT NULL,
    "Version" bigint NOT NULL,
    CONSTRAINT "PK_platform_plan" PRIMARY KEY ("PlanCode")
);

CREATE TABLE IF NOT EXISTS platform_tenant_subscription (
    "TenantId" uuid NOT NULL,
    "SubscriptionId" uuid NOT NULL,
    "PlanCode" character varying(48) NOT NULL,
    "PlanVersion" bigint NOT NULL,
    "Status" character varying(24) NOT NULL,
    "StartsAt" timestamp with time zone NOT NULL,
    "EndsAt" timestamp with time zone,
    "GraceEndsAt" timestamp with time zone,
    "IsCurrent" boolean NOT NULL,
    "Version" bigint NOT NULL,
    "TermsSnapshot" jsonb NOT NULL,
    CONSTRAINT "PK_platform_tenant_subscription" PRIMARY KEY ("TenantId", "SubscriptionId")
);

CREATE TABLE IF NOT EXISTS platform_tenant_entitlement (
    "TenantId" uuid NOT NULL,
    "ModuleCode" character varying(48) NOT NULL,
    "Source" character varying(24) NOT NULL,
    "IsEnabled" boolean NOT NULL,
    "EffectiveFrom" timestamp with time zone NOT NULL,
    "EffectiveUntil" timestamp with time zone,
    CONSTRAINT "PK_platform_tenant_entitlement" PRIMARY KEY ("TenantId", "ModuleCode")
);

CREATE TABLE IF NOT EXISTS platform_plan_module (
    "PlanCode" character varying(48) NOT NULL,
    "ModuleCode" character varying(48) NOT NULL,
    CONSTRAINT "PK_platform_plan_module" PRIMARY KEY ("PlanCode", "ModuleCode")
);

CREATE TABLE IF NOT EXISTS platform_tenant_api_usage (
    "TenantId" uuid NOT NULL,
    "MonthStart" date NOT NULL,
    "RequestCount" bigint NOT NULL,
    CONSTRAINT "PK_platform_tenant_api_usage" PRIMARY KEY ("TenantId", "MonthStart")
);

COMMIT;
