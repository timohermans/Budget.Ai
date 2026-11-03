"""Preserve the existing normalized-name index under its original name.

Django's model index metadata rejects names longer than 30 characters, so this
index is created with SQL in both fresh and adopted databases.
"""

from __future__ import annotations

from django.db import migrations


class Migration(migrations.Migration):
    dependencies = [
        ("transactions", "0001_initial"),
    ]

    operations = [
        migrations.RunSQL(
            sql=(
                "CREATE INDEX IF NOT EXISTS ix_transactions_name_other_party_normalized "
                "ON transactions (name_other_party_normalized);"
            ),
            reverse_sql=(
                "DROP INDEX IF EXISTS ix_transactions_name_other_party_normalized;"
            ),
        ),
    ]
