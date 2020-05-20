# UnityPlayerLogAnalyzer
Analyze player log output from standalone player and output to nice YAML format.

![Alt text](./DemoImage.png)

So when you do alpha or beta testing, you as programmer get many log files from play testers.
This program will help you analyze these log files easier, with help of your favorite text editing program, of course!

## Feature:
- Reinterpret log file into YAML which can collapse/expand node and most text editor has nice coloring.
- Remove normal message log entirely from input file, if you only interest in warning/error.
- Option to collapse or show only unique error line, even if such line appear 100 times it will be collapsed to one.