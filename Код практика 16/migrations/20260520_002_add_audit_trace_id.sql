DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM information_schema.tables
        WHERE table_schema = 'public'
          AND table_name = 'audit_log'
    ) AND NOT EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'public'
          AND table_name = 'audit_log'
          AND column_name = 'trace_id'
    ) THEN
        ALTER TABLE public.audit_log
            ADD COLUMN trace_id varchar(128);
    END IF;

    IF EXISTS (
        SELECT 1
        FROM information_schema.tables
        WHERE table_schema = 'public'
          AND table_name = 'audit_log'
    ) THEN
        CREATE INDEX IF NOT EXISTS ix_audit_log_trace_id
            ON public.audit_log (trace_id);

        COMMENT ON COLUMN public.audit_log.trace_id
            IS 'Distributed trace identifier associated with the audited operation.';
    END IF;
END $$;
