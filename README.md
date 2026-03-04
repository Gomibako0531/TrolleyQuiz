# Trolley Quiz Game (Unity)

A Unity-based quiz game inspired by the "Trolley Adventure" segment from the Japanese TV show *Nep League*.  
Multiple teams answer two-choice questions, and their trolley moves left or right depending on their answer.

This project was developed as an interactive game for group activities.

---

# Game Overview

Players are divided into **8 teams (A–H)**.  
Each team is represented by a **colored trolley**.

For each question, teams choose between **two options (Left / Right)**.

After all answers are collected, the trolleys move toward the side they selected, and the correct side is revealed.

The game continues for multiple questions, and teams earn points for correct answers.

At the end, the **top three teams are announced**.

---

# Game Flow

The game progresses through several scenes:


### 0. TitleScene
- Displays the title
- Button to start

### 1. RunScene
- Displays the quiz question
- Shows two answer choices (text or image)
- 30-second time limit
- Teams discuss and choose their answer
- Operator inputs answers on the PC (A or D keys)

### 2. SplitScene
- Trolleys move forward along a 30° angled track
- Teams split into left and right paths based on their answer
- A short animation plays for suspense

### 3. BranchScene
- Correct side is revealed
- "Correct!" / "Incorrect" text appears
- Lightning effect strikes the incorrect side
- Points are awarded

### 4. ResultScene
- Final rankings are calculated
- Top 3 teams are displayed

---

# Features

- 8-team quiz system
- Randomized correct answer side
- Team-based scoring system
- Dynamic trolley spawning
- Ranking based on correct answers
- Lightning particle effects for incorrect answers
- Scene-based game flow
- UI scaling for different screen sizes
- Supports text or image answer choices

---

# Controls

| Key | Action |
|----|------|
| A | Select Left answer |
| D | Select Right answer |
| Space | Proceed to next phase |

---

＃ Author

Fujita Ryusei
Ritsumeikan University
