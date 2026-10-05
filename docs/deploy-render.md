# Free demo deployment on Render

The root `Dockerfile` builds React and ASP.NET Core into one web image. ASP.NET Core serves the React files, API, uploaded images, and SignalR hub on the same origin. PostgreSQL runs as a separate managed database.

## Deploy

1. Push this branch to GitHub.
2. In the [Render dashboard](https://dashboard.render.com/), choose **New > Blueprint** and connect this repository and branch. Render reads `render.yaml` and proposes one free Docker web service plus one free PostgreSQL database.
3. Review the proposed resources and deploy. The Blueprint generates the JWT secret and an inaccessible random password for the seeded demo accounts. Migrations, sample categories, and sample lots are created when the app starts.
4. Open the web service's `onrender.com` URL. Verify `/`, `/lots/1`, and `/api/health`. Register a new user to try the authenticated features.
5. Submit the web service URL as the hosting link.

No `VITE_API_URL` or `VITE_HUB_URL` is needed: the browser uses `/api` and `/hubs/auction` on the same host. The container listens on port `10000`, which is Render's default web-service port.

## Free-plan limits

The free Render PostgreSQL database expires after 30 days and has no backups. Free web services have an ephemeral filesystem, so files uploaded to `/uploads` disappear after a restart or redeploy. For this demo, use externally hosted image URLs when creating lots if images must remain visible. A longer-lived deployment needs a paid database and persistent image storage.

Keep all credentials in Render environment variables. Do not add a local `.env` file to the Docker image or repository.
