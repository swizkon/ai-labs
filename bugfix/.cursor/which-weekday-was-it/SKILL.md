---
name: which-weekday-was-it
description: A skill to determine the weekday for a given date. Used this skill when you need to find out which day of the week a specific date falls on.
---

# Which Weekday Was It

## When to use this skill
Use this skill when you need to find out which day of the week a specific date falls on. For example, if you want to know what day of the week July 4, 2021, was, you can use this skill to quickly determine that it was a Sunday. This can be helpful for planning events, scheduling appointments, or simply satisfying your curiosity about historical dates.

## How to extract text
1. Send a web request to the API endpoint with the date you want to check.

Example request:
```
GET http://localhost:5288/weekday/2021-07-04
```
This will tell you that July 4, 2021, was a Sunday.
