# Build Vocaluxe - HowTo (Windows)

1. Navigate to the right repository: usually [Vocaluxe Main](https://github.com/Vocaluxe/Vocaluxe)
2. Navigate to the right branch: usually [develop](https://github.com/Vocaluxe/Vocaluxe/tree/develop) (The main branch of Vocaluxe)
  
  ![branch](https://cloud.githubusercontent.com/assets/5115160/4995706/bc5aa536-69c2-11e4-8534-b64573624a33.PNG)

3. Copy the `URL` of the corresponding git file
  
  ![url](https://cloud.githubusercontent.com/assets/5115160/4995708/bc5af234-69c2-11e4-91d1-112972f35f01.PNG)

4. Make sure git is installed [git-scm](http://git-scm.com/)
5. Open a git bash (there should be a link in the start menu)
6. Navigate to the folder where git should create the Vocaluxe project folder
  ```
  cd [PathToYourFolder]
  ```
7. Clone the repository (could take some time...) 
  ```
  git clone [theCopiedGitUrl]
  ```
  ![clone](https://cloud.githubusercontent.com/assets/5115160/4995707/bc5aa6da-69c2-11e4-95cb-71ac7b0c3fc8.png)

8. Now you should find a Folder `Vocaluxe` in the given directory -> navigate into it
9. Make sure [Visual Studio 2012/2013(?)](http://www.visualstudio.com/downloads/download-visual-studio-vs#d-express-windows-desktop) is installed 
10. Open the file `Vocaluxe.sln` with Visual Studio
11. Configure the build target to `ReleaseWin` and `x86` or `x64` (if unsure -> select `x86`)
12. Build the projects by clicking `"Build"->"Build solution"` or just press `Control`+`Shift`+`B`
  
  ![build](https://cloud.githubusercontent.com/assets/5115160/4995709/bc5b3474-69c2-11e4-9d9b-b49f8a3b315d.png)

13. Check if your build was successful
  
  ![buildsuccess](https://cloud.githubusercontent.com/assets/5115160/4995710/bc5b4fe0-69c2-11e4-8b0f-4acaeffbc568.png)

14. YOUR build should have appeared here:
  ```
  [PathToYourFolder]\Vocaluxe\Output
  ```
15. Run `Vocaluxe.exe` or copy the whole folder to a location of your choice
16. Have fun! 
