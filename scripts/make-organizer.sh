#!/usr/bin/env bash
set -euo pipefail

EMAIL="${1:-}"

if [[ -z "$EMAIL" ]]; then
  echo "Usage: $0 <email>"
  exit 1
fi

PGPASSWORD=admin psql -h localhost -U postgres -d electric_ai -c "
UPDATE plans.\"Users\"
SET \"Role\" = 'Organizer', \"UpdatedUtc\" = NOW() AT TIME ZONE 'UTC'
WHERE \"Email\" = '$EMAIL'
RETURNING \"Id\", \"Email\", \"Role\";
"
