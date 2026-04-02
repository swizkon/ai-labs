---
name: info-about-historical-people
description: A skill to get information about historical people. Use this skill when you need to find out information about a person's life, achievements, or historical context.
---

# Info About Historical People

## When to use this skill
Use this skill when you need to find out information about a historical person's life, achievements, or historical context. For example, if you want to know about Napoleon Bonaparte, you can use this skill to quickly gather information about his life and accomplishments. This can be helpful for research, education, or satisfying your curiosity about historical figures.

## How to extract text
1. Send a web request to the API endpoint with the name of the historical person you want to learn about.

Example request 1:
```
GET https://en.wikipedia.org/wiki/Napoleon
```
This will provide information about "Napoleon".

Example request 2:
```
GET https://en.wikipedia.org/wiki/Linus_Torvalds
```
This will provide information about "Linus Torvalds".
So use underscore when there is a spaces in the name.

2. Extract relevant information from the response, such as birth and death dates, major achievements, and historical context.
3. Use the extracted information to answer questions or provide insights about the historical person.
