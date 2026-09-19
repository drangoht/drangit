// Enrichissement du prompt de l'accueil (ADR 0008).
//
// Le socle est un formulaire GET qui fonctionne sans ce fichier : chaque commande de filtre
// ou de navigation est une vraie requête, et une adresse partageable. Ce script n'ajoute que
// du confort — la sortie qui s'empile sans recharger, l'historique, la complétion — et se
// retire sans rien emporter d'essentiel.
//
// Il ne connaît ni texte ni règle métier. Les blocs de sortie sont rendus par le serveur,
// traduits et déjà chiffrés ; le script les recopie. Ajouter une commande, c'est ajouter un
// bloc dans la page : rien ici n'a besoin de le savoir.
(function () {
    "use strict";

    var form = document.querySelector(".cli__form");
    var outputs = document.querySelector(".cli__outputs");

    if (!form || !outputs) {
        return;
    }

    var input = form.querySelector("input[name='c']");
    var prompt = form.querySelector(".cli__prompt");
    var screen = document.querySelector(".window__screen");
    var initial = document.querySelector(".cli__initial");
    var log = document.createElement("div");
    var tried = Object.create(null);

    // L'historique survit au rechargement : une commande qui filtre ou qui ouvre une fiche
    // est une vraie navigation (ADR 0008), et la page repart de zéro à chaque fois. Sans
    // cela, la flèche du haut ne rappellerait que les commandes sans effet de bord.
    // sessionStorage plutôt que localStorage : un historique de terminal appartient à la
    // session, pas au navigateur, et il n'a rien à faire là dans six mois.
    var HISTORY_KEY = "drangit.console.history";
    var HISTORY_MAX = 50;

    var history = read();
    var historyIndex = history.length;

    function read() {
        try {
            var stored = JSON.parse(window.sessionStorage.getItem(HISTORY_KEY) || "[]");
            return Array.isArray(stored) ? stored : [];
        } catch (error) {
            // Navigation privée, stockage refusé, contenu abîmé : l'historique est un
            // confort, son absence ne doit rien empêcher.
            return [];
        }
    }

    function remember(line) {
        // Comme un shell qui ignore les doublons : rappeler trois fois « ls » n'aide personne.
        if (history[history.length - 1] !== line) {
            history.push(line);
        }

        if (history.length > HISTORY_MAX) {
            history = history.slice(-HISTORY_MAX);
        }

        historyIndex = history.length;

        try {
            window.sessionStorage.setItem(HISTORY_KEY, JSON.stringify(history));
        } catch (error) {
            // Rien à faire : la commande, elle, part quand même.
        }
    }

    log.className = "cli__log";

    // Dans l'écran, à la suite de ce qui y est déjà écrit : la sortie appartient au
    // terminal. L'insérer entre l'écran et le prompt la mettrait hors de la zone qui
    // défile, et elle s'empilerait sous la fenêtre.
    (screen || form.parentNode).appendChild(log);

    // Les raccourcis sont annoncés seulement maintenant : sans ce fichier, ni Tab ni Ctrl+L
    // ne font quoi que ce soit, et les afficher d'emblée promettrait ce qui n'existe pas.
    var hints = document.querySelector(".cli__hints");

    if (hints) {
        hints.hidden = false;
    }

    // Alias qui n'existent que pour le confort de frappe : le serveur, lui, n'en connaît
    // aucun — ce sont des commandes qui ne le concernent pas.
    var ALIASES = {
        "?": "help",
        "cls": "clear",
        "fun": "eggs",
        "secrets": "eggs",
        "easter": "eggs",
        "quit": "exit",
        "logout": "exit",
    };

    /** Commandes traitées ici même, qui n'ont pas de bloc de sortie à afficher. */
    var LOCAL = ["clear", "history"];

    function nameOf(line) {
        var first = line.trim().split(/\s+/)[0].toLowerCase();
        return ALIASES[first] || first;
    }

    function outputFor(name) {
        return outputs.querySelector("[data-output='" + (window.CSS && CSS.escape ? CSS.escape(name) : name) + "']");
    }

    function append(node) {
        log.appendChild(node);

        // L'écran suit sa dernière ligne, comme un terminal qui déroule.
        if (screen) {
            screen.scrollTop = screen.scrollHeight;
        } else {
            node.scrollIntoView({ block: "nearest" });
        }
    }

    function echo(line) {
        var element = document.createElement("p");
        element.className = "cli__echo";
        element.textContent = (prompt ? prompt.textContent.trim() + " " : "") + line;
        append(element);
    }

    function write(text, className) {
        var element = document.createElement("pre");
        element.className = className || "";
        element.textContent = text;
        append(element);
    }

    /**
     * Neuf lignes de glyphes, pour le seul bloc qui demande un effet. Elles sont écrites,
     * pas animées : quarante images par seconde derrière un texte ne se lisent pas, et
     * l'effet tiendrait de la nuisance plus que du clin d'œil.
     */
    function rain() {
        var glyphes = "アイウエオカキクケコサシスセソタチツテト01001101";
        var lines = [];

        for (var row = 0; row < 9; row++) {
            var line = "";

            for (var column = 0; column < 58; column++) {
                line += Math.random() < 0.22
                    ? " "
                    : glyphes.charAt(Math.floor(Math.random() * glyphes.length));
            }

            lines.push(line);
        }

        write(lines.join("\n"), "cli__rain");
    }

    /** Marque d'un signe les commandes inutiles déjà essayées, et compte ce qui reste. */
    function markEggs(node) {
        var items = node.querySelectorAll("[data-egg]");
        var found = 0;

        for (var index = 0; index < items.length; index++) {
            var item = items[index];

            if (tried[item.getAttribute("data-egg")]) {
                item.classList.add("cli__egg--found");
                found++;
            }
        }

        node.setAttribute("data-found", found + "/" + items.length);
    }

    function show(name, node) {
        var copy = node.cloneNode(true);

        copy.removeAttribute("hidden");
        copy.classList.add("cli__output");

        if (name === "eggs") {
            markEggs(copy);
        }

        if (node.getAttribute("data-effect") === "rain") {
            rain();
        }

        append(copy);
    }

    /**
     * Rend la main au formulaire quand la commande ne se traite pas ici : filtrer, ouvrir une
     * fiche, changer de page sont des navigations, et c'est le serveur qui les décide.
     */
    /**
     * Vide l'écran entier, et pas seulement ce que le prompt y a ajouté : ce que le serveur
     * avait écrit — bannière, liste, compteur — en fait partie. La prochaine commande qui
     * navigue recharge la page et le remet, comme un « ls » après un « clear ».
     */
    function clear() {
        log.replaceChildren();

        if (initial) {
            initial.hidden = true;
        }
    }

    function handle(line) {
        var name = nameOf(line);

        if (!name) {
            return false;
        }

        if (name === "clear") {
            clear();
            return true;
        }

        var node = outputFor(name);

        if (!node && LOCAL.indexOf(name) < 0) {
            return false;
        }

        echo(line);
        tried[name] = true;

        if (name === "history") {
            write(history.length
                ? history.map(function (entry, index) { return "  " + (index + 1) + "  " + entry; }).join("\n")
                : "");
            return true;
        }

        show(name, node);
        return true;
    }

    /** Vocabulaire de la complétion, lu dans la page : rien n'est répété ici. */
    function candidates(word) {
        var pool = LOCAL.slice();

        outputs.querySelectorAll("[data-output]").forEach(function (node) {
            pool.push(node.getAttribute("data-output"));
        });

        if (word.indexOf("--topic=") === 0) {
            pool = [];
            document.querySelectorAll(".chips .chip").forEach(function (chip) {
                pool.push("--topic=" + chip.firstChild.textContent.trim());
            });
        } else if (word.indexOf("--lang=") === 0) {
            pool = [];
            document.querySelectorAll("#lang option[value]:not([value=''])").forEach(function (option) {
                pool.push("--lang=" + option.getAttribute("value"));
            });
        } else if (word.charAt(0) === "-") {
            pool = ["--topic=", "--lang=", "--demo", "--no-archived"];
        } else {
            document.querySelectorAll(".listing__name").forEach(function (cell) {
                pool.push(cell.textContent.replace(/^\s*\S+\//, "").trim());
            });
        }

        return pool.filter(function (candidate) {
            return candidate.toLowerCase().indexOf(word.toLowerCase()) === 0;
        });
    }

    function complete() {
        var parts = input.value.split(/\s+/);
        var word = parts[parts.length - 1];

        if (!word) {
            return;
        }

        var matches = candidates(word);

        if (matches.length === 1) {
            parts[parts.length - 1] = matches[0];
            input.value = parts.join(" ");
        } else if (matches.length > 1) {
            echo(input.value);
            write(matches.slice(0, 16).join("   "), "cli__hint");
        }
    }

    form.addEventListener("submit", function (event) {
        var line = input.value.trim();

        if (line) {
            // Avant le départ du formulaire : quand la commande navigue, ce code ne sera
            // plus là pour le faire.
            remember(line);
        }

        if (handle(line)) {
            event.preventDefault();
            input.value = "";
        }
    });

    // Cliquer dans l'écran rend la main au prompt, comme dans un terminal — sauf si l'on
    // vient de sélectionner du texte, auquel cas on voulait le copier, pas taper.
    if (screen) {
        screen.addEventListener("mouseup", function () {
            if (!String(window.getSelection())) {
                input.focus();
            }
        });
    }

    // Le curseur est dans la ligne de commande dès l'arrivée : on ouvre un terminal pour y
    // taper. `preventScroll` évite que la page saute jusqu'au champ, qui est en bas du cadre.
    input.focus({ preventScroll: true });

    input.addEventListener("keydown", function (event) {
        if (event.key === "ArrowUp" && historyIndex > 0) {
            event.preventDefault();
            historyIndex--;
            input.value = history[historyIndex];
        } else if (event.key === "ArrowDown") {
            event.preventDefault();
            historyIndex = Math.min(historyIndex + 1, history.length);
            input.value = historyIndex < history.length ? history[historyIndex] : "";
        } else if (event.key === "Tab") {
            event.preventDefault();
            complete();
        } else if (event.key === "l" && event.ctrlKey) {
            event.preventDefault();
            clear();
        }
    });
}());
