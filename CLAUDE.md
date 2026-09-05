# Workflow

- NEVER run interactive browser tools, headless browsers, or app simulations
  (Playwright, Chromium, Puppeteer, screenshot drivers, etc.) unless the user
  explicitly asks for it in that request. Verify work with builds, typechecks,
  tests, and lint instead. If a visual check seems needed, ask first.
- If we add/change a feature that is relevant to either README.md or the homepage, make sure to update them after
- NEVER commit or push or create/change git branches 

# UI rules

These apply to all user-facing text: labels, descriptions, tooltips, dialog
messages, empty states, error messages, etc.

- No em dashes (—) in user-facing text. Use a period, comma, or colon instead.
- Don't explain functionality the UI already makes obvious. If a checkbox,
  button, or field's purpose is clear from its label and context, skip the
  helper text. Only add a description when it conveys something the user
  couldn't infer: a non-obvious consequence, a constraint, or where to find
  something external.
- When a description is warranted, keep it to one short sentence. Cut
  restatements of the label, hedging, and background explanation.
