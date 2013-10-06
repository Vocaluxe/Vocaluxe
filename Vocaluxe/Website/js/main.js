var ownProfileId = -1;
var profileIdRequest = -1;
var songIdRequest = -1
var allSongsCache = null;

$(document).ready(function () {
    replaceTransitionHandler();
    initPageLoadHandler();
    initKeyboardPageHandler();
    initMainPageHandler();
    initLoginPageHandler();
});

function replaceTransitionHandler() {
    //Thx to http://stackoverflow.com/a/14096311
    var oldDefaultTransitionHandler = $.mobile.defaultTransitionHandler;

    $.mobile.defaultTransitionHandler = function (name, reverse, $to, $from) {
        var promise = $to.data('promise');
        if (promise) {
            $to.removeData('promise');
            $.mobile.loading('show');
            return promise.then(function () {
                $.mobile.loading('hide');
                return oldDefaultTransitionHandler(name, reverse, $to, $from);
            });
        }
        return oldDefaultTransitionHandler(name, reverse, $to, $from);
    };
}

function initPageLoadHandler() {
    //pageLoadHandler for displayProfile
    $(document).on('pagebeforeshow', '#displayProfile', function () {
        var promise = $.ajax({
            url: "getProfile?profileId=" + profileIdRequest
        }).done(function (result) {
            $('#playerName').prop("value", result.PlayerName);
            if (result.Avatar && result.Avatar.base64Data) {
                $('#playerAvatar').prop("src", result.Avatar.base64Data);
            }
            $('#playerType').prop("value", result.Type);
            $('#playerDifficulty').prop("value", result.Difficulty);
            if (result.IsEditable) {
                $('#playerAvatar').click(function () {
                    if ($('#captureContainer').length > 0) {
                        $('#captureContainer').remove();
                    }

                    $(document.body).append('<div id="captureContainer" style="height: 0px;width:0px; overflow:hidden;"> <input type="file" accept="image/*" id="capture" capture> </div>');

                    $('#capture').change(function (eventData) {
                        if (eventData && eventData.target && eventData.target.files && eventData.target.files.length == 1) {
                            var file = eventData.target.files[0];
                            var reader = new FileReader();
                            reader.onloadend = function (e) {
                                $('#playerAvatar').prop("src", e.target.result);
                                $('#playerAvatar').data("changed", true);
                                $('#captureContainer').remove();
                            };
                            reader.readAsDataURL(file);
                        }
                    });

                    $('#capture').click();
                });

                $('#playerSaveButton').click(function () {
                    var dataToUpload = {};

                    dataToUpload["ProfileId"] = profileIdRequest;
                    dataToUpload["PlayerName"] = $('#playerName').prop("value");
                    dataToUpload["Type"] = $('#playerType').prop("value");
                    dataToUpload["Difficulty"] = $('#playerDifficulty').prop("value");
                    dataToUpload["Avatar"] = $('#playerAvatar').data("changed") ? { "base64Data": $('#playerAvatar').prop("src") } : null;

                    $('#content').wrap('<div class="overlay" />');
                    $.mobile.loading('show', {
                        text: 'Uploading profile...',
                        textVisible: true
                    });

                    $.ajax({
                        url: "sendProfile",
                        dataType: "json",
                        contentType: "application/json;charset=utf-8",
                        type: "POST",
                        data: JSON.stringify(dataToUpload),
                        success: function (msg) {

                        }
                    }).always(function () {
                        $.mobile.loading('hide');
                        $('#content').unwrap();
                    });

                });
            }
            else {
                $('#playerName').prop('disabled', true);
                $('#playerType').prop('disabled', true);
                $('#playerDifficulty').prop('disabled', true);
                $('#playerSaveButton').hide();
                $('#playerAvatar').unbind("click");
            }
        });

        // Save promise on page so the transition handler can find it.
        $(this).data('promise', promise);
    });

    //pageLoadHandler for selectProfile
    $(document).on('pagebeforeshow', '#selectProfile', function () {
        var promise = $.ajax({
            url: "getProfileList"
        }).done(function (data) {
            $('#selectProfileList').children().remove();

            function handleProfileSelectLineClick(e) {
                profileIdRequest = parseInt(e.currentTarget.id.replace("ProfileSelectLine_", ""));
                $.mobile.changePage("#displayProfile", { transition: "slidefade" });
            }

            for (var profile in data) {
                $('<li id="ProfileSelectLine_' + data[profile].ProfileId + '"> <a href="#"> <img src="' + ((data[profile].Avatar && data[profile].Avatar.base64Data) ? data[profile].Avatar.base64Data : "img/profile.png") + '"> <h2>' + data[profile].PlayerName + '</h2> <p>Click here to show the profile of ' + data[profile].PlayerName + '</p> </a> </li>')
                    .appendTo('#selectProfileList')
                    .click(handleProfileSelectLineClick);
            }

            $('#selectProfileList').listview('refresh');
        });

        // Save promise on page so the transition handler can find it.
        $(this).data('promise', promise);
    });

    //pageLoadHandler for displaySong
    $(document).on('pagebeforeshow', '#displaySong', function () {
        var promise = $.ajax({
            url: "getSong?songId=" + songIdRequest
        }).done(function (result) {
            if (result.Title != null) {
                $('#displaySongTitle').text(result.Title);
            }
            else {
                $('#displaySongTitle').text("No current song");
            }

            if (result.Cover && result.Cover.base64Data) {
                $('#displaySongCover').prop("src", result.Cover.base64Data);
            }
            else {
                $('#displaySongCover').prop("src", "img/noCover.png");
            }

            if (result.Artist != null) {
                $('#displaySongArtist').text(result.Artist);
            }
            else {
                $('#displaySongArtist').text("-");
            }

            if (result.Genre != null) {
                $('#displaySongGenre').text(result.Genre);
            }
            else {
                $('#displaySongGenre').text("-");
            }

            if (result.Year != null && result.Year != "") {
                $('#displaySongYear').text(result.Year);
            }
            else {
                $('#displaySongYear').text("-");
            }

            if (result.Language != null) {
                $('#displaySongLanguage').text(result.Language);
            }
            else {
                $('#displaySongLanguage').text("-");
            }

            $('#displaySongIsDuet').text(result.IsDuet ? "Yes" : "No");
        });

        // Save promise on page so the transition handler can find it.
        $(this).data('promise', promise);
    });

    //pageLoadHandler for selectSong
    $(document).on('pagebeforeshow', '#selectSong', function () {
        function handleGetAllSongs() {
            $('#selectSongList').children().remove();

            function handleSelectSongLineClick(e) {
                songIdRequest = parseInt(e.currentTarget.id.replace("selectSongLine_", ""));
                $.mobile.changePage("#displaySong", { transition: "slidefade" });
            }

            for (var id in allSongsCache) {
                $('<li id="selectSongLine_' + allSongsCache[id].SongId + '"> <a href="#"> '/*+'<img src="' + ((data[profile].Avatar && data[profile].Avatar.base64Data) ? data[id].Avatar.base64Data : "img/profile.png") + '"> '*/ + ' <h2>' + allSongsCache[id].Artist + '</h2> <p>' + allSongsCache[id].Title + '</p> </a> </li>')
                    .appendTo('#selectSongList')
                    .click(handleSelectSongLineClick);
            }

            $('#selectSongList').listview('refresh');
        }

        if (allSongsCache == null) {
            var promise = $.ajax({
                url: "getAllSongs"
            }).done(function (data) {
                allSongsCache = data;
                handleGetAllSongs()
            });

            // Save promise on page so the transition handler can find it.
            $(this).data('promise', promise);
        }
        else {
            handleGetAllSongs()
        }
    });
}

function initLoginPageHandler() {
    $('#loginButton').click(function () {
        ownProfileId = parseInt($('#playerId').prop("value"));
        if (ownProfileId != "NaN") {
            $.mobile.changePage("#main", { transition: "slidefade" });
        }
    });
}

function initMainPageHandler() {
    $('#yourProfileLink').click(function () {
        profileIdRequest = ownProfileId;
        $.mobile.changePage("#displayProfile", { transition: "slidefade" });
    });

    $('#currentSongLink').click(function () {
        $('#content').wrap('<div class="overlay" />');
        $.mobile.loading('show', {
            text: 'Getting current song...',
            textVisible: true
        });

        $.ajax({
            url: "getCurrentSongId"
        }).done(function (result) {
            songIdRequest = parseInt(result);
            $.mobile.loading('hide');
            $('#content').unwrap();
            $.mobile.changePage("#displaySong", { transition: "slidefade" });
        }).fail(function (result) {
            $.mobile.loading('hide');
            $('#content').unwrap();
        });        
    });

    $('#mainPageTakePhotoLink').click(function () {
        if ($('#captureContainer').length > 0) {
            $('#captureContainer').remove();
        }

        $(document.body).append('<div id="captureContainer" style="height: 0px;width:0px; overflow:hidden;"> <input type="file" accept="image/*" id="capture" capture="camera"> </div>');

        $('#capture').change(function (eventData) {
            if (eventData && eventData.target && eventData.target.files && eventData.target.files.length == 1) {
                $('#content').wrap('<div class="overlay" />');
                $.mobile.loading('show', {
                    text: 'Uploading photo...',
                    textVisible: true
                });

                var file = eventData.target.files[0];
                var reader = new FileReader();

                reader.onloadend = function (e) {
                    $.ajax({
                        url: "sendPhoto",
                        dataType: "json",
                        contentType: "application/json;charset=utf-8",
                        type: "POST",
                        data: JSON.stringify({ Photo: { base64Data: e.target.result } }),
                        success: function (msg) {

                        }
                    }).always(function () {
                        $.mobile.loading('hide');
                        $('#content').unwrap();
                    });
                };

                reader.readAsDataURL(file);
            }
        });

        $('#capture').click();
    });
}

function initKeyboardPageHandler() {
    $('#keyboardButtonUp').click(function () {
        $.ajax({
            url: "sendKeyEvent?key=up"
        })
    });

    $('#keyboardButtonDown').click(function () {
        $.ajax({
            url: "sendKeyEvent?key=down"
        })
    });

    $('#keyboardButtonLeft').click(function () {
        $.ajax({
            url: "sendKeyEvent?key=left"
        })
    });

    $('#keyboardButtonRight').click(function () {
        $.ajax({
            url: "sendKeyEvent?key=right"
        })
    });

    $('#keyboardButtonEscape').click(function () {
        $.ajax({
            url: "sendKeyEvent?key=escape"
        })
    });

    $('#keyboardButtonkeyboardButtonTab').click(function () {
        $.ajax({
            url: "sendKeyEvent?key=tab"
        })
    });

    $('#keyboardButtonReturn').click(function () {
        $.ajax({
            url: "sendKeyEvent?key=return"
        })
    });

    $('#keyboardButtonKeys').keyup(function (e) {
        var c = String.fromCharCode(e.keyCode);
        if (c.match(/\w/)) {
            c = e.keyCode >= 65 ? c.toLowerCase() : c;
            $.ajax({
                url: "sendKeyEvent?key=" + c
            })
        }
        var oldText = $('#keyboardButtonKeys')[0].value;
        if (oldText.length > 0) {
            $('#keyboardButtonKeys')[0].value = oldText.slice(1);
        }
    });
}
