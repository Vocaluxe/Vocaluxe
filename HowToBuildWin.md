# Build Vocaluxe - HowTo (Windows)

* Navigate to the right repository: usually [Vocaluxe Main](https://github.com/Vocaluxe/Vocaluxe)
* Navigate to the right branch: usually [develop](https://github.com/Vocaluxe/Vocaluxe/tree/develop) (The main branch of Vocaluxe)
  
  ![branch](https://cloud.githubusercontent.com/assets/5115160/4995706/bc5aa536-69c2-11e4-8534-b64573624a33.PNG)

* Copy the `URL` of the corresponding git file
  
  ![url](https://cloud.githubusercontent.com/assets/5115160/4995708/bc5af234-69c2-11e4-91d1-112972f35f01.PNG)

* Make sure [Visual Studio 2012/2013(?)](http://www.visualstudio.com/downloads/download-visual-studio-vs#d-express-windows-desktop) is installed 

* Clone the repository and open it

    **Method A: Use Visual Studio (2013+)'s git client:**
    * Start Visual Studio
    * `VIEW`->`Team Explorer`
    * Click on the `Connect to a Team Project` button inside the `Team Explorer` 
    * Click on `Clone` under `Local Git Repositories`
    * Enter `[theCopiedGitUrl]` in the first field
    * Enter a target folder in the second field
    * Click clone
    ![vsclone](https://cloud.githubusercontent.com/assets/5115160/4998350/de81ca76-69d7-11e4-983d-d2886aa9faa0.png)

 **Method B: Use commandline git client:**
    * Make sure git is installed [git-scm](http://git-scm.com/)
    * Open a `git bash` (there should be a link in the start menu) 
    or a normal commandline if you have added git to the `PATH` variable
    * Navigate to the folder where git should create the Vocaluxe project folder
    
      ```
      cd [PathToYourFolder]
      ```
    * Clone the repository (could take some time...) 
      ```
      git clone [theCopiedGitUrl]
      ```
      ![clone](https://cloud.githubusercontent.com/assets/5115160/4995707/bc5aa6da-69c2-11e4-95cb-71ac7b0c3fc8.png)
    
    * Now you should find a Folder `Vocaluxe` in the given directory -> navigate into it
    
    * Open the file `Vocaluxe.sln` with Visual Studio

* Configure the build target to `ReleaseWin` and `x86` or `x64` (if unsure -> select `x86`)
* Build the projects by clicking `"Build"->"Build solution"` or just press `Control`+`Shift`+`B`
  
  ![build](https://cloud.githubusercontent.com/assets/5115160/4995709/bc5b3474-69c2-11e4-9d9b-b49f8a3b315d.png)

* Check if your build was successful
  ![buildsuccess](https://cloud.githubusercontent.com/assets/5115160/4995710/bc5b4fe0-69c2-11e4-8b0f-4acaeffbc568.png)

* YOUR build should have appeared here:
  ```
  [PathToYourFolder]\Vocaluxe\Output
  ```
* Run `Vocaluxe.exe` or copy the whole folder to a location of your choice
* Have fun! 
