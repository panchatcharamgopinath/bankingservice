# Event Management — Backlog (P0 Epics & Stories)

Epic: Event Authoring & Publishing
- Story E1-S1: As Event Manager, create an Event record with metadata, sessions, pricing, capacity, and publish status.
  - Acceptance: Event saved in Dataverse, sessions linked, publish toggles set; preview landing page shows event details.
- Story E1-S2: As Event Manager, edit or cancel event; notify registrants on change.
  - Acceptance: Edits saved; Power Automate sends update email to all registered contacts.

Epic: Sessions & Scheduling
- Story E2-S1: As Event Manager, add multiple Sessions to an Event with timezone-aware start/end and capacity.
  - Acceptance: Sessions appear under Event; times stored with timezone; capacity enforced on registration.
- Story E2-S2: As System, detect scheduling conflicts for assigned speakers and block double-booking.
  - Acceptance: Conflicting assignment prevented; UI shows conflict warning.

Epic: Speaker Portal (Phase 1 — MVP)
- Story E3-S1: As Speaker, register/login to Speaker Portal and set availability windows.
  - Acceptance: Speaker availability stored; portal accessible via Power Pages; Entra authentication works.
- Story E3-S2: As Speaker, accept/decline assigned sessions and upload presentation materials.
  - Acceptance: Acceptance recorded; materials saved to Blob storage and linked to session record; Event Manager notified.

Epic: Registration & Payments (Visa & MasterCard)
- Story E4-S1: As Attendee, complete registration with contact data and select sessions/tickets.
  - Acceptance: Registration record created, seat reserved (pending payment).
- Story E4-S2: As Attendee, pay with Visa/MasterCard via tokenized gateway (Stripe/Adyen) and receive confirmation.
  - Acceptance: Payment completed, gateway token stored (not card data), transaction recorded in Dataverse with status 'Paid'; confirmation email with join link.
- Story E4-S3: As System, handle payment webhook retries and reconcile failed payments.
  - Acceptance: Failed payments retried; registration status set to 'Payment Failed' and attendee notified.

Epic: Attendance Capture & Check-in
- Story E5-S1: As System, capture virtual session join/leave timestamps from meeting provider and persist Attendance records.
  - Acceptance: Join/leave recorded within 15 minutes of session end; attendance linked to registration.
- Story E5-S2: As Onsite Staff, check-in attendees via QR scan (Power Apps) for physical sessions.
  - Acceptance: Scan creates attendance record and updates capacity counts.

Epic: Ratings & Feedback
- Story E6-S1: As Attendee, submit session rating and feedback post-session via email link.
  - Acceptance: Rating and comment stored; rating appears in session dashboard.
- Story E6-S2: As System, send reminder if no rating within 48 hours (configurable).
  - Acceptance: Reminder sent only to attendees without ratings.

Epic: Reporting & Dashboards
- Story E7-S1: As Event Manager, view dashboard for registrations, revenue, attendance, and ratings.
  - Acceptance: Dashboard shows real-time counts and export option (CSV) for current event.
- Story E7-S2: As Finance, export payment transactions for reconciliation.
  - Acceptance: Export contains transactionId, registrationId, contactId, amount, gatewayRef, status.

Epic: Notifications & Automation
- Story E8-S1: As System, send registration confirmation, calendar invite (Outlook), and reminders.
  - Acceptance: Emails sent; calendar invite contains meeting join link and unique join token.

Epic: Integrations & Security
- Story E9-S1: As Admin, configure Teams/Zoom integration and validate webhook permissions.
  - Acceptance: Meeting provider webhooks authenticated and producing session join events in dev environment.
- Story E9-S2: As Admin, ensure PCI scope minimized (no card data stored) and EntraID auth enabled for portals.
  - Acceptance: Payment flow uses tokenized charges; security review checklist completed.

Cross-cutting Acceptance Criteria (applies to all P0 stories)
- All customer-facing pages are WCAG 2.1 AA compliant.
- Consent captured at registration for marketing and data-retention policies.
- All operations working for initial scale of 100 attendees.
