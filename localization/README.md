# Start guide for translators

Required software:
- [git](https://git-scm.com/)
- [github cli](https://cli.github.com/)
- [poedit](https://poedit.com/)
- [godot](https://godotengine.org/download/)

Other requirements:
- [github](https://github.com/) account

## Configure git and your fork

First thing you will have to do is configure git (given you have already made/have a github account).

Set your username.

```shell
git config --global user.name <username>
```

Then set your email.

```shell
git config --global user.email <email>
```

Use github cli to setup authentication to github, run

```shell
gh auth login
```

and follow the instructions.
  
<br>

Now it's time to make a fork of the repo.

First make sure you're logged into your github account and visit https://github.com/Rhythia/Client.git.

Click on "fork" to create your fork and make sure "Copy the master branch only" is disabled.

Now clone your fork with the following command.

```shell
git clone https://github.com/<your_username>/<your_fork_name_probably_Client>.git
```

Now enter the Client folder that was just downloaded

```shell
cd <your_fork_name_probably_Client>
```

and switch to the indev branch.

```shell
git switch indev
```

Now you can choose to use poedit or gettext to translate as seen below.

## How to make a new translation with poedit (windows, linux, macos)

This is the easier way of doing it (compared to gettext).

To make a new translation file:
1. open poedit
2. open the .pot file: Navigate to the project folder you cloned earlier, inside open `localization` and select `template.pot`
3. click on the "Create new translation" button
4. select your language
5. click on the "Create new translation" button
6. (optional) go to File -> Preferences and add your name and email to have it put in the "Last-Translator" field in the header.
7. start translating!

Click on the first entry and on the bottom you will see 2 fields: "Source Text" and "Translation", put your text in translation. Do this for each entry.

Once you're done click on the save icon and save the file in the `localization` folder with the name `<language>_<COUNTRY>.po`.

## Push and PR your translations

Before pushing your changes open the project in godot, go to "Project -> Project Settings -> Localization -> Add" and add the .po file you have just created.

Now that you are done translating it's time to add these translations into the game.

To do that open the cloned folder in your terminal.

Use the following command to stage all changes

```shell
git add .
```

and make a commit.

```shell
git commit -m "i18n: add <language> translations"
```

Now push to your fork with the following command:

```shell
git push
```

Now head over to [github](https://github.com/) and open your fork.

Switch the branch from "master" to "indev".

On top of the folder like view you should see the most recent commit and a bar saying something like "This branch is n commits ahead of Rhythia/Client:indev" click on "Contribute" and "Open pull request"

Make sure "base repository" is **Rhythia/Client** and "base" is **indev**.

In the title put `i18n: add <language> translations`.

Finally click on "Create pull request" and wait for maintainers to check out your changes.

## Additional info

Throughout the translation file you will find placeholders like `{n}` (n being a number), above the key you should find a comment explaining what the placeholder will contain, feel free to move the placeholder around to match your language's syntactic rules.

In translations containing placeholders use double curly brackets for literal curly brackets.

# Info for contributors

If you add strings to template.pot make sure to alphabetically sort them with the following command (gettext) from within the localization directory:

```shell
msgcat --sort-output --no-wrap template.pot -o template_sorted.pot  && rm template.pot && mv template_sorted.pot template.pot
```
