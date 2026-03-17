\# 02 - Business Flow



\## Core Flow

Term setup

\-> CourseOffering / ClassSection setup

\-> Enrollment

\-> Finalize roster

\-> Handoff exam scheduling

\-> Attendance

\-> Grades

\-> Transcript / reporting



\## Core Business Concepts

\- StudentClass = lớp hành chính

\- CourseOffering / ClassSection = lớp học phần

\- Enrollment is the source of truth for studying eligibility



\## Key Rules

\- Students self-enroll on web

\- Admin/Staff can enroll on behalf of students

\- All enrollment actions go through one Enrollment Engine

\- Checks:

&#x20; - prerequisite

&#x20; - time conflict

&#x20; - capacity

&#x20; - credit limit

&#x20; - repeat rule

\- Override must be auditable

\- Late enrollment after finalize is allowed only if related downstream data can be synchronized safely

