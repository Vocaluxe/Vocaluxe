# VocaluxeWebsite
This branch contains our GitHub-Page. It's accessible via https://vocaluxe.github.io and based on Jekyll, a popular static site generator.

##Working on this website
For easier working this website and don't mess around with all other branches, we suggest to clone this as a single-branch. Just follow these steps:

1. Clone only `gh-pages` branch: `git clone git@github.com:Vocaluxe/Vocaluxe.git -b gh-pages --single-branch VocaluxeWeb`
2. Rename remote: `git remote rename origin upstream`
3. Add your forked Vocaluxe repository as remote: `git remote add origin git@github.com:USERNAME/Vocaluxe.git`
4. Push `gh-pages` to your repository: `git push origin gh-pages`
5. Checkout your `gh-pages` branch: `git checkout origin/gh-pages`

Your now able to make some changes, create commits and open pull-request. When pulling or pushing some changes, we recommand to specify your pull. Otherwise you would download the whole vocaluxe project and waste some space. `git pull origin gh-pages` for pulling your own changes.

##Opening issues and pull-request
We're happy when you have some ideas or find bugs on our website. Please add the `website`-label when opening a new issue or pullrequest.
