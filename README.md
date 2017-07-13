# VocaluxeWebsite
This branch contains our GitHub-Page. It's accessible via https://vocaluxe.github.io/Vocaluxe/ and based on [Jekyll](https://jekyllrb.com/), a popular static site generator.

## Working on this website
For easier working on this website and to not mess around with all other branches, we suggest to clone this as a single-branch. Just follow these steps:

1. Clone only the `gh-pages` branch: `git clone https://github.com/Vocaluxe/Vocaluxe.git -b gh-pages --single-branch VocaluxeWeb`
2. Rename remote: `git remote rename origin upstream`
3. Add your forked Vocaluxe repository as remote: `git remote add origin https://github.com/USERNAME/Vocaluxe.git`
4. Push `gh-pages` to your repository: `git push origin gh-pages`
5. Checkout your `gh-pages` branch: `git checkout origin/gh-pages -B gh-pages`

You're now able to make some changes, create commits and open pull-request. When pulling or pushing some changes, we recommend to specify your branch. Otherwise you would download the whole Vocaluxe project and waste some disc space. Use `git pull origin gh-pages` for pulling your own changes.

## Opening issues and pull requests
We're happy if you have some ideas or find bugs on our website. Please add the `website`-label when opening a new issue or pull request.
