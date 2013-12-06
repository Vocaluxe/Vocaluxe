var ownProfileId = -1;
var profileIdRequest = -1;
var songIdRequest = -1;
var allSongsCache = null;
var playlistIdRequest = -1;
var playlistRequestName = "";
var customSelectPlaylistSongCallback = null;
var sessionId = "";
var serverBaseAddress = "";
var tranlationLoaded;

if (document.location.protocol == "file:") {
    if (typeof (window.deviceAndJqmLoaded) == "undefined") {
        window.deviceAndJqmLoaded = $.Deferred();
    }
    window.deviceAndJqmLoaded.done(function () {
        preStart();
    });
} else {
    $(document).ready(function () {
        preStart();
    });
}

function preStart() {
    tranlationLoaded = $.Deferred();
    
    initTranslation();

    tranlationLoaded.done(function () {
        translate();
        start();
    });
}

function start() {
    replaceTransitionHandler();
    initPageLoadHandler();
    initKeyboardPageHandler();
    initMainPageHandler();
    initDiscoverPageHandler();
    initLoginPageHandler();
    initHeartbeat();
    initVideoPopup();
}

function replaceTransitionHandler() {
    //Thx to http://stackoverflow.com/a/14096311
    var oldDefaultTransitionHandler = $.mobile.defaultTransitionHandler;

    $.mobile.defaultTransitionHandler = function (name, reverse, $to, $from) {
        var promise = $to.data('promise');
        if (promise) {
            $to.removeData('promise');
            /*$('div[data-role="content"]').wrap('<div class="overlay" />');
            $.mobile.loading('show', {
                text: 'Loading data...',
                textVisible: true
            });*/
            return promise.then(function () {
                /*$.mobile.loading('hide');
                $('div[data-role="content"]').unwrap();*/
                return oldDefaultTransitionHandler(name, reverse, $to, $from);
            });
        }
        return oldDefaultTransitionHandler(name, reverse, $to, $from);
    };
}

function initPageLoadHandler() {
    //pageLoadHandler for discover
    $(document).on('pagebeforeshow', '#discover', pagebeforeshowDiscover);

    //pageLoadHandler for displayProfile
    $(document).on('pagebeforeshow', '#displayProfile', function () {
        if (profileIdRequest >= 0) {
            var promise = request({
                url: "getProfile?profileId=" + profileIdRequest
            }).done(function (result) {
                handleDisplayProfileData(result);

                $('#playerSaveButton').click(function () {
                    var dataToUpload = {};

                    dataToUpload["ProfileId"] = profileIdRequest;
                    dataToUpload["PlayerName"] = $('#playerName').prop("value");
                    dataToUpload["Type"] = $('#playerType').prop("value");
                    dataToUpload["Difficulty"] = $('#playerDifficulty').prop("value");
                    dataToUpload["Avatar"] = $('#playerAvatar').data("changed") ? { "base64Data": $('#playerAvatar').prop("src") } : null;

                    var pass = $('#playerPassword').prop("value");
                    if (pass != "***__oldPassword__***") {
                        if (pass == "") {
                            dataToUpload["Password"] = "***__CLEAR_PASSWORD__***";
                        } else {
                            dataToUpload["Password"] = pass;
                        }
                    } else {
                        dataToUpload["Password"] = null;
                    }

                    request({
                        url: "sendProfile",
                        contentType: "application/json;charset=utf-8",
                        type: "POST",
                        data: JSON.stringify(dataToUpload),
                    }, "Uploading profile...").done(function () {
                        history.back();
                    });

                });
            });

            // Save promise on page so the transition handler can find it.
            $(this).data('promise', promise);
        }
        else {
            //new profile
            handleDisplayProfileData({
                "Avatar": { "base64Data": null },
                "Difficulty": 0,
                "IsEditable": true,
                "PlayerName": i18n.t("YourName") || "YourName",
                "ProfileId": -1,
                "Type": 1
            });
            $('#playerType').prop("value", 0);
            $('#playerDifficulty').prop("value", 0);

            $('#playerSaveButton').click(function () {
                var dataToUpload = {};

                dataToUpload["ProfileId"] = -1;
                dataToUpload["PlayerName"] = $('#playerName').prop("value");
                dataToUpload["Type"] = $('#playerType').prop("value");
                dataToUpload["Difficulty"] = $('#playerDifficulty').prop("value");
                dataToUpload["Avatar"] = $('#playerAvatar').data("changed") ? { "base64Data": $('#playerAvatar').prop("src") } : null;

                var pass = $('#playerPassword').prop("value");
                if (pass != "***__oldPassword__***") {
                    if (pass == "") {
                        dataToUpload["Password"] = "***__CLEAR_PASSWORD__***";
                    } else {
                        dataToUpload["Password"] = pass;
                    }
                } else {
                    dataToUpload["Password"] = null;
                }

                request({
                    url: "sendProfile",
                    contentType: "application/json;charset=utf-8",
                    type: "POST",
                    data: JSON.stringify(dataToUpload)
                }, 'Creating profile...').done(function () {
                    $.mobile.changePage("#login", { transition: "slidefade" });
                });

            });
        }
    });

    function handleDisplayProfileData(data) {
        $('#playerName').prop("value", data.PlayerName);

        addImage($('#playerAvatar')[0], data.Avatar, "img/profile.png");

        $('#playerAvatar').data("changed", false);
        $('#playerType').prop("selectedIndex", data.Type).selectmenu("refresh");
        $('#playerDifficulty').prop("selectedIndex", data.Difficulty).selectmenu("refresh");
        if (data.IsEditable) {
            $('#playerName').prop('disabled', false);
            $('#playerType').prop('disabled', false);
            $('#playerDifficulty').prop('disabled', false);
            $('#playerSaveButton').show().unbind("click");
            $('#playerAvatar').unbind("click");
            $('#playerPassword').prop('disabled', false).prop("value", "***__oldPassword__***").parent().show();
            $('#playerPasswordLabel').show();

            $('#playerAvatar').click(function () {
                if ($('#captureContainer').length > 0) {
                    $('#captureContainer').remove();
                }

                if (document.location.protocol == "file:"
                    && typeof (navigator) != 'undefined'
                    && typeof (navigator.camera) != 'undefined'
                    && typeof (navigator.camera.getPicture) != 'undefined') {
                    navigator.camera.getPicture(function (imageData) {
                        $('#playerAvatar').prop("src", "data:image/jpeg;base64," + imageData);
                        $('#playerAvatar').data("changed", true);
                    }, function () {
                        //Fail - do nothing
                    }, {
                        destinationType: navigator.camera.DestinationType.DATA_URL,
                        allowEdit: true,
                        correctOrientation: true,
                        saveToPhotoAlbum: true,
                        quality: 50
                    });

                } else {
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
                }
            });
        }
        else {
            $('#playerName').prop('disabled', true);
            $('#playerType').prop('disabled', true);
            $('#playerDifficulty').prop('disabled', true);
            $('#playerSaveButton').hide().unbind("click");
            $('#playerAvatar').unbind("click");
            $('#playerPassword').prop('disabled', true).parent().hide();
            $('#playerPasswordLabel').hide();
        }
    }

    //pageLoadHandler for selectProfile
    $(document).on('pagebeforeshow', '#selectProfile', function () {
        var promise = request({
            url: "getProfileList"
        }).done(function (data) {
            $('#selectProfileList').children().remove();

            function handleProfileSelectLineClick(e) {
                profileIdRequest = parseInt(e.currentTarget.id.replace("ProfileSelectLine_", ""));
                $.mobile.changePage("#displayProfile", { transition: "slidefade" });
            }

            for (var profile in data) {
                var img = $('<li id="ProfileSelectLine_' + data[profile].ProfileId + '"> <a href="#"> <img> <h2>' + data[profile].PlayerName + '</h2> <p>Click here to show the profile of ' + data[profile].PlayerName + '</p> </a> </li>')
                    .appendTo('#selectProfileList')
                    .click(handleProfileSelectLineClick)
                    .find("img")[0];

                addImage(img, data[profile].Avatar, "img/profile.png");
            }

            $('#selectProfileList').listview('refresh');
        });

        // Save promise on page so the transition handler can find it.
        $(this).data('promise', promise);
    });

    //pageLoadHandler for displaySong
    $(document).on('pagebeforeshow', '#displaySong', function () {
        var promise = request({
            url: "getSong?songId=" + songIdRequest
        }).done(function (result) {
            $('#displaySongAddPlaylist').hide();

            if (result.Title != null) {
                $('#displaySongTitle').text(result.Title);
            }
            else {
                $('#displaySongTitle').text("No current song");
            }

            addImage($('#displaySongCover')[0], result.Cover, "img/noCover.png");

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

            if (result.Title != null || result.Artist != null) {
                $('#displaySongLinks').show();

                $('#displaySongAddPlaylist').unbind('click').click(function () {
                    customSelectPlaylistSongCallback = function (playlistId) {
                        request({ url: "addSongToPlaylist?songId=" + result.SongId + "&playlistId=" + playlistId + "&duplicates=false" }, "Add to playlist...");
                        history.back();
                    };
                    $.mobile.changePage("#selectPlaylist", { transition: "slidefade" });
                });

                $('#displaySongLinkYoutube').unbind('click').click(function () {
                    showYoutube(result.Artist, result.Title);
                });

                $('#displaySongLinkSpotify').unbind('click').click(function () {
                    showSpotify(result.Artist, result.Title);
                });

                $('#displaySongLinkWikipedia').unbind('click').click(function () {
                    showWikipedia(result.Artist);
                });
            } else {
                $('#displaySongLinks').hide();
            }

            request({
                url: "hasUserRight?right=" + 32
            }).done(function (result2) {
                if (result2) {
                    $('#displaySongAddPlaylist').show();
                } else {
                    $('#displaySongAddPlaylist').hide();
                }
            });
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
            var promise = request({
                url: "getAllSongs"
            }).done(function (data) {
                allSongsCache = data;
                handleGetAllSongs();
            });

            // Save promise on page so the transition handler can find it.
            $(this).data('promise', promise);
        }
        else {
            handleGetAllSongs();
        }
    });

    //pageLoadHandler for selectUserAdmin
    $(document).on('pagebeforeshow', '#selectUserAdmin', function () {
        var promise = request({
            url: "getProfileList"
        }).done(function (data) {
            $('#selectUserAdminList').children().remove();

            function handleSelectUserAdminLineClick(e) {
                profileIdRequest = parseInt(e.currentTarget.id.replace("SelectUserAdminLine_", ""));
                $.mobile.changePage("#displayUserAdmin", { transition: "slidefade" });
            }

            for (var profile in data) {
                $('<li id="SelectUserAdminLine_' + data[profile].ProfileId + '"> <a href="#"> <img src="' + ((data[profile].Avatar && data[profile].Avatar.base64Data) ? data[profile].Avatar.base64Data : "img/profile.png") + '"> <h2>' + data[profile].PlayerName + '</h2> <p>Click here to edit roles for ' + data[profile].PlayerName + '</p> </a> </li>')
                    .appendTo('#selectUserAdminList')
                    .click(handleSelectUserAdminLineClick);
            }

            $('#selectUserAdminList').listview('refresh');
        });

        // Save promise on page so the transition handler can find it.
        $(this).data('promise', promise);
    });

    //pageLoadHandler for displayUserAdmin
    $(document).on('pagebeforeshow', '#displayUserAdmin', function () {
        var promise = request({
            url: "getUserRole?profileId=" + profileIdRequest
        }).done(function (result) {
            $('#roleAdministrator').prop("checked", ((result & 0x01) != 0)).checkboxradio("refresh");

            $('#btnRoleSave').unbind('click').click(function () {
                var role = 0;
                if ($('#roleAdministrator').prop("checked")) {
                    role = (role | 0x01);
                }
                request({
                    url: "setUserRole?profileId=" + profileIdRequest + "&userRole=" + role,
                    headers: { "session": sessionId }
                }, "Saving...").done(function (result) {
                    history.back();
                });
            });
        });

        // Save promise on page so the transition handler can find it.
        $(this).data('promise', promise);
    });

    //pageLoadHandler for login
    $(document).on('pagebeforeshow', '#login', pagebeforeshowLogin);

    //pageLoadHandler for main
    $(document).on('pagebeforeshow', '#main', function () {
        request({
            url: "hasUserRight?right=" + 2
        }).done(function (result) {
            if (result) {
                $('#mainPageTakePhotoLink').parent().parent().parent().show();
            } else {
                $('#mainPageTakePhotoLink').parent().parent().parent().hide();
            }
        });

        request({
            url: "hasUserRight?right=" + 8
        }).done(function (result) {
            if (result) {
                $('#mainPageKeyboard').parent().parent().parent().show();
            } else {
                $('#mainPageKeyboard').parent().parent().parent().hide();
            }
        });

        request({
            url: "hasUserRight?right=" + 4
        }).done(function (result) {
            if (result) {
                $('#mainPageSelectProfile').parent().parent().parent().show();
            } else {
                $('#mainPageSelectProfile').parent().parent().parent().hide();
            }
        });
    });

    //pageLoadHandler for selectPlaylist
    $(document).on('pagebeforeshow', '#selectPlaylist', function () {
        function handleGetAllPlaylists(data) {
            $('#selectPlaylistContentList').children().remove();
            $('#selectPlaylistAddPlaylistButton').hide();

            function handleSelectSongLineClick(e) {
                playlistIdRequest = parseInt(e.currentTarget.parentElement.parentElement.parentElement.id.replace("selectPlaylistLine_", ""));
                playlistRequestName = $(e.currentTarget.parentElement.parentElement.parentElement).find('h2').text();
                if (customSelectPlaylistSongCallback != null) {
                    customSelectPlaylistSongCallback(playlistIdRequest, playlistRequestName);
                    customSelectPlaylistSongCallback = null;
                } else {

                    $.mobile.changePage("#displayPlaylist", { transition: "slidefade" });
                }
            }

            function handleSelectSongLineDeleteClick(e) {
                var playlistToDeleteId = parseInt(e.currentTarget.parentElement.id.replace("selectPlaylistLine_", ""));
                var playlistToDeleteName = $(e.currentTarget.parentElement).find('h2').text();
                if (window.confirm("Do you really want to delete this playlist:\n" + playlistToDeleteName)) {
                    request({ url: "removePlaylist?playlistId=" + playlistToDeleteId }, "Loading...").done(function () {
                        request({
                            url: "getPlaylists"
                        }).done(function (data2) {
                            handleGetAllPlaylists(data2);
                        });
                    });
                }
            }

            for (var id in data) {
                var line = $('<li id="selectPlaylistLine_'
                    + data[id].PlaylistId
                    + '"> <a href="#"> '/*+'<img src="' + ((data[profile].Avatar && data[profile].Avatar.base64Data) ? data[id].Avatar.base64Data : "img/profile.png") + '"> '*/
                    + ' <h2>' + data[id].PlaylistName + '</h2> <p>'
                    + data[id].SongCount
                    + ' ' + i18n.t('songs') + '</p> </a> <a href="#" class="delete" data-icon="delete">Delete</a> </li>')
                    .appendTo('#selectPlaylistContentList');
                line.find('a:not(.delete)').click(handleSelectSongLineClick);
                line.find('a.delete').click(handleSelectSongLineDeleteClick);
            }

            $('#selectPlaylistContentList').listview('refresh');

            request({
                url: "hasUserRight?right=" + 128
            }).done(function (result) {
                if (result) {
                    $('#selectPlaylistContentList').find('.delete').show();
                    $('#selectPlaylistContentList').listview('refresh');
                } else {
                    $('#selectPlaylistContentList').find('.delete').hide();
                    $('#selectPlaylistContentList').listview('refresh');
                }
            });

            $('#selectPlaylistAddPlaylistButton').unbind('click').hide().click(function () {
                var name = prompt("Name of the new playlist:", "NewPlaylistName");
                if (name != null
                  && name.replace(" ", "") != ""
                  && $('h2').filter(function () { return this.textContent == name; }).length == 0) {
                    request({ url: "addPlaylist?playlistName=" + name }, "Creating...").done(function () {
                        request({
                            url: "getPlaylists"
                        }).done(function (data2) {
                            handleGetAllPlaylists(data2);
                        });
                    });
                } else {
                    alert(i18n.t("This is not a valid name."));
                }
            });

            request({
                url: "hasUserRight?right=" + 64
            }).done(function (result) {
                if (result) {
                    $('#selectPlaylistAddPlaylistButton').show();
                } else {
                    $('#selectPlaylistAddPlaylistButton').hide();
                }
            });
        }

        var promise = request({
            url: "getPlaylists"
        }).done(function (data) {
            handleGetAllPlaylists(data);
        });

        // Save promise on page so the transition handler can find it.
        $(this).data('promise', promise);

    });

    //pageLoadHandler for displayPlaylist
    $(document).on('pagebeforeshow', '#displayPlaylist', function () {
        function handleGetAllPlaylistSongs(data) {
            $('#displayPlaylistHeader').find('h1').text(playlistRequestName);

            $('#displayPlaylistSaveButton').hide().unbind('click').click(function () {
                var promises = [];
                $('#displayPlaylistContentList').find('li').each(function (indx, elem) {
                    var newPos = $(elem).index();
                    var oldPos = $(elem).data("oldPos");
                    if (newPos != oldPos) {
                        var movedId = elem.id.replace("selectSongLine_", "");

                        promises.push(request({
                            url: "moveSongInPlaylist?&newPosition=" + newPos + "&playlistId=" + playlistIdRequest + "&songId=" + movedId
                        }, "Resorting..."));
                    }
                });

                $.when.apply(promises).done(function () {
                    //reload data
                    request({
                        url: "getPlaylistSongs?playlistId=" + playlistIdRequest
                    }).done(function (data2) {
                        handleGetAllPlaylistSongs(data2);
                    });
                });
            });

            $('#displayPlaylistContentList').children().remove();

            function handleSelectPlaylistSongLineClick(e) {
                songIdRequest = parseInt(e.currentTarget.parentElement.parentElement.parentElement.id.replace("selectSongLine_", ""));
                $.mobile.changePage("#displaySong", { transition: "slidefade" });
            }

            function handleSelectPlaylistSongLineDeleteClick(e) {
                var songToDeleteId = parseInt(e.currentTarget.parentElement.id.replace("selectSongLine_", ""));
                var songToDeleteName = $(e.currentTarget.parentElement).find('h2').text();

                if (window.confirm("Do you really want to delete this song from playlist:\n" + songToDeleteName)) {
                    request({
                        url: "/removeSongFromPlaylist?position=" + $('#selectSongLine_' + songToDeleteId).index()
                            + "&playlistId=" + playlistIdRequest
                            + "&songId=" + songToDeleteId
                    }, "Loading...").done(function () {
                        //reload data
                        request({
                            url: "getPlaylistSongs?playlistId=" + playlistIdRequest
                        }).done(function (data2) {
                            handleGetAllPlaylistSongs(data2);
                        });
                    });
                }


            }

            var sortedData = [];

            for (var id1 in data) {
                sortedData[data[id1].PlaylistPosition] = data[id1].Song;
            }
            var i = 0;
            for (var id in sortedData) {
                var line = $('<li id="selectSongLine_'
                    + sortedData[id].SongId
                    + '"> <a href="#"> <img> <h2>'
                    + sortedData[id].Artist
                    + '</h2> <p>'
                    + sortedData[id].Title
                    + '</p> </a> <a href="#" class="delete" data-icon="delete">Delete</a> </li>')
                    .appendTo('#displayPlaylistContentList').data("oldPos", i++);

                line.find('a:not(.delete)').click(handleSelectPlaylistSongLineClick);
                line.find('a.delete').click(handleSelectPlaylistSongLineDeleteClick);

                var img = line.find("img")[0];

                addImage(img, sortedData[id].Cover, "img/noCover.png");
            }

            $('#displayPlaylistContentList').listview('refresh');

            request({
                url: "hasUserRight?right=" + 256
            }).done(function (result) {
                if (result) {
                    $('#displayPlaylistContentList').find('.delete').show();
                    $('#displayPlaylistContentList').listview('refresh');
                } else {
                    $('#displayPlaylistContentList').find('.delete').hide();
                    $('#displayPlaylistContentList').listview('refresh');
                }
            });

            request({
                url: "hasUserRight?right=" + 16
            }).done(function (result) {
                if (result) {
                    $('#displayPlaylistContentList').sortable({
                        axis: 'y',
                        sort: function () {
                            $('#displayPlaylistSaveButton').show();

                            var $lis = $(this).children('li');
                            $lis.each(function () {
                                var $li = $(this);
                                var hindex = $lis.filter('.ui-sortable-helper').index();
                                if (!$li.is('.ui-sortable-helper')) {
                                    var index = $li.index();
                                    index = index < hindex ? index + 1 : index;

                                    $li.val(index);

                                    if ($li.is('.ui-sortable-placeholder')) {
                                        $lis.filter('.ui-sortable-helper').val(index);
                                    }
                                }
                            });
                        }
                    });
                }
            });
        }

        var promise = request({
            url: "getPlaylistSongs?playlistId=" + playlistIdRequest
        }).done(function (data) {
            handleGetAllPlaylistSongs(data);
        });

        // Save promise on page so the transition handler can find it.
        $(this).data('promise', promise);

    });
}

function pagebeforeshowLogin() {
    if (window.localStorage) {
        var value = window.localStorage.getItem("VocaluxeSessionKey");
        if (value != null && value != "") {
            sessionId = value;
        }
    }

    if (sessionId != "") {
        if (ownProfileId != -1) {
            $.mobile.changePage("#main", { transition: "slidefade" });
        }
        else {
            request({
                url: "getOwnProfileId",
                headers: { "session": sessionId }
            }, 'Login...').done(function (result) {
                ownProfileId = result;
                $.mobile.changePage("#main", { transition: "slidefade" });
            });
        }
    }
}

function pagebeforeshowDiscover() {
    if (document.location.protocol == "file:") {
        if (window.localStorage) {
            var address = window.localStorage.getItem("VocaluxeServerAddress");
            if (address != null) {
                serverBaseAddress = "";
                var prom = request({
                    url: address + "isServerOnline",
                    timeout: 10000
                }, "Checking...").done(function () {
                    serverBaseAddress = address;
                    window.localStorage.setItem("VocaluxeServerAddress", address);
                    $.mobile.changePage("#login", { transition: "none" });
                }).fail(function () {
                    if (typeof window.BarcodeScanner != "undefined") {
                        $('#discoverReadQr').show();
                    }
                });
                $(this).data('promise', prom);
                return;
            }
            if (typeof window.BarcodeScanner != "undefined") {
                $('#discoverReadQr').show();
            }
        }
    } else {
        $('#discoverReadQr').hide();
        $.mobile.changePage("#login", { transition: "none" });
    }
}

function initLoginPageHandler() {
    var keyPressed = function (e) {
        if (e.which == 13) {
            $('#loginButton').click();
        }
    };

    $('#loginName').keypress(keyPressed);
    $('#loginPassword').keypress(keyPressed);

    $('#loginButton').click(function () {
        var username = $('#loginName').prop("value");
        var password = $('#loginPassword').prop("value");

        request({
            url: "login?username=" + username + "&password=" + password
        }).done(function (result) {
            sessionId = result;
            if (window.localStorage) {
                window.localStorage.setItem("VocaluxeSessionKey", sessionId);
            }
            request({
                url: "getOwnProfileId",
                headers: { "session": sessionId }
            }, 'Login...').done(function (result2) {
                ownProfileId = result2;
                $.mobile.changePage("#main", { transition: "slidefade" });
            });
        });
    });

    $('#registerButton').click(function () {
        ownProfileId = -1;
        profileIdRequest = -1;
        $.mobile.changePage("#displayProfile", { transition: "slidefade" });
    });

    //Fire pageLoadHandler for login
    //pagebeforeshowLogin();
}

function initMainPageHandler() {
    $('#yourProfileLink').click(function () {
        profileIdRequest = ownProfileId;
        $.mobile.changePage("#displayProfile", { transition: "slidefade" });
    });

    $('#currentSongLink').click(function () {
        request({
            url: "getCurrentSongId",
            headers: { "session": sessionId }
        }, 'Getting current song...').done(function (result) {
            songIdRequest = parseInt(result);
            $.mobile.changePage("#displaySong", { transition: "slidefade" });
        });
    });

    function uploadImg(imgData) {
        request({
            url: "sendPhoto",
            contentType: "application/json;charset=utf-8",
            type: "POST",
            data: JSON.stringify({ Photo: { base64Data: imgData } })
        }, 'Uploading photo...');
    }

    $('#mainPageTakePhotoLink').click(function () {
        if ($('#captureContainer').length > 0) {
            $('#captureContainer').remove();
        }

        if (document.location.protocol == "file:"
            && typeof (navigator) != 'undefined'
            && typeof (navigator.camera) != 'undefined'
            && typeof (navigator.camera.getPicture) != 'undefined') {
            navigator.camera.getPicture(function (imageData) {
                uploadImg("data:image/jpeg;base64," + imageData);
            }, function () {
                //Fail - do nothing
            }, {
                destinationType: navigator.camera.DestinationType.DATA_URL,
                allowEdit: true,
                correctOrientation: true,
                saveToPhotoAlbum: true,
                quality: 100
            });

        } else {
            $(document.body).append('<div id="captureContainer" style="height: 0px;width:0px; overflow:hidden;"> <input type="file" accept="image/*" id="capture" capture="camera"> </div>');

            $('#capture').change(function (eventData) {
                if (eventData && eventData.target && eventData.target.files && eventData.target.files.length == 1) {
                    var file = eventData.target.files[0];
                    var reader = new FileReader();

                    reader.onloadend = function (e) {
                        uploadImg(e.target.result);
                        $('#capture').remove();
                    };

                    reader.readAsDataURL(file);
                }
            });

            $('#capture').click();
        }
    });

    $('#mainPageLogoutLink').click(function () {
        logout();
    });
}

function initKeyboardPageHandler() {
    $('#keyboardButtonUp').click(function () {
        request({
            url: "sendKeyEvent?key=up"
        });
    });

    $('#keyboardButtonDown').click(function () {
        request({
            url: "sendKeyEvent?key=down"
        });
    });

    $('#keyboardButtonLeft').click(function () {
        request({
            url: "sendKeyEvent?key=left"
        });
    });

    $('#keyboardButtonRight').click(function () {
        request({
            url: "sendKeyEvent?key=right"
        });
    });

    $('#keyboardButtonEscape').click(function () {
        request({
            url: "sendKeyEvent?key=escape"
        });
    });

    $('#keyboardButtonTab').click(function () {
        request({
            url: "sendKeyEvent?key=tab"
        });
    });

    $('#keyboardButtonReturn').click(function () {
        request({
            url: "sendKeyEvent?key=return"
        });
    });

    $('#keyboardButtonKeys').keyup(function (e) {
        var c = String.fromCharCode(e.keyCode);
        if (c.match(/\w/)) {
            c = e.keyCode >= 65 ? c.toLowerCase() : c;
            request({
                url: "sendKeyEvent?key=" + c
            });
        }
        var oldText = $('#keyboardButtonKeys')[0].value;
        if (oldText.length > 0) {
            $('#keyboardButtonKeys')[0].value = oldText.slice(1);
        }
    });
}

function initDiscoverPageHandler() {
    var keyPressed = function (e) {
        if (e.which == 13) {
            $('#discoverConnect').click();
        }
    };

    $('#discoverServerAddress').keypress(keyPressed);

    function handleAddress(address) {
        if (address != null && address != "") {
            if (address.indexOf("http") != 0) {
                address = "http://" + address;
            }
            if (address.slice(-1) != '/') {
                address = address + '/';
            }
            serverBaseAddress = "";
            request({
                url: address + "isServerOnline",
                timeout: 10000
            }, "Checking...")
                .done(function () {
                    serverBaseAddress = address;
                    window.localStorage.setItem("VocaluxeServerAddress", address);
                    $.mobile.changePage("#login", { transition: "slidefade" });
                })
                .fail(function () {
                    $('#discoverServerAddress').prop("value", "");
                    if (document.location.protocol == "file:"
                        && typeof window.BarcodeScanner != "undefined") {
                        $('#discoverReadQr').show();
                    }
                });
        }
    }

    $('#discoverConnect').click(function () {
        handleAddress($('#discoverServerAddress').prop("value"));
    });

    try {
        window.BarcodeScanner = cordova.require("cordova/plugin/BarcodeScanner");
    } catch (e) { }

    $('#discoverReadQr').hide().click(function () {
        window.BarcodeScanner.scan(
          function (result) {
              handleAddress(result.text);
          },
          function () {
              showError("Scan faild");
          }
       );
    });

    //Fire pageLoadHandler for discover (first page shown after start)
    setTimeout(function () {
        pagebeforeshowDiscover();
        $(this).removeData('promise');
    }, 1);
}

var cachedImages = {};

function delayedImageLoad(elem, id, fail) {
    if (elem && id) {
        if (cachedImages[id]) {
            elem.src = cachedImages[id].base64Data;
            return;
        }

        elem.src = "";
        $(elem).addClass("imageLoaderImg");
        request({
            url: "delayedImage?id=" + id
        }).done(function (result) {
            $(elem).removeClass("imageLoaderImg");
            elem.src = result.base64Data;
            cachedImages[id] = result;
        }).fail(function () {
            $(elem).removeClass("imageLoaderImg");
            if (fail) {
                elem.src = fail;
            }
        });
    }
}

function addImage(img, base64Image, defaultImg) {
    if (base64Image && (base64Image.base64Data || base64Image.imageId)) {
        if (base64Image.base64Data) {
            $(img).prop("src", base64Image.base64Data);
        }
        else {
            delayedImageLoad(img, base64Image.imageId, defaultImg);
        }
    }
    else {
        $(img).prop("src", defaultImg);
    }
}

function logout() {
    ownProfileId = -1;
    profileIdRequest = -1;
    songIdRequest = -1;
    sessionId = "";
    if (window.localStorage) {
        window.localStorage.setItem("VocaluxeSessionKey", "");
    }
    $.mobile.changePage("#login", { transition: "slidefade" });
}

function checkSession() {
    if (ownProfileId == -1
        && profileIdRequest == -1
        && ($.mobile.activePage.attr("id") == "displayProfile" || $.mobile.activePage.attr("id") == "login" || $.mobile.activePage.attr("id") == "discover")) {
        return;
    }
    request({
        url: "getOwnProfileId"
    }, "noOverlay").done(function (result) {
        if (result == -1) {
            logout();
        }
    }).fail(function (result) {
        logout();
    });
}

function initHeartbeat() {
    setInterval(checkSession, 20000);
}

function request(data, message) {
    if ((typeof message) == "undefined" || message == null) {
        message = "Loading...";
    }

    if ((typeof data.timeout) == "undefined") {
        data.timeout = 10000; //10 sec. timeout
    }

    if (message != "noOverlay") {
        var message2 = message;
        if (i18n.t) {
            message2 = i18n.t(message2);
        }
        $('div[data-role="content"]').wrap('<div class="overlay" />');
        $.mobile.loading('show', {
            text: message2,
            textVisible: true
        });
    }

    if (!data["headers"]) {
        data["headers"] = {};
    }

    if (message == "external") {
        message = "Loading (external)...";
        if (i18n.t) {
            message = i18n.t(message) || message;
        }
    } else {
        data["headers"]["session"] = sessionId;
        data.url = serverBaseAddress + data.url;
    }

    return $.ajax(data).always(function (result) {
        if (message != "noOverlay") {
            $.mobile.loading('hide');
            $('div[data-role="content"]').unwrap();
        }
    }).fail(function (result) {
        if (result.statusText.indexOf("No session") != -1) {
            logout();
            return;
        }
        if (message != "noOverlay") {
            var msg = result.statusText.length <= 20 ? result.statusText : "Error...";
            if (msg == "error" && result.readyState == 0) {
                msg = "No connection";
            }
            showError(msg);
        }
    });
}

function showError(message) {
    if (!message) {
        message = "Error...";
    }

    if (i18n.t) {
        message = i18n.t(message) || message;
    }

    $('div[data-role="content"]').wrap('<div class="overlay" />');
    $.mobile.loading('show', {
        text: message,
        textVisible: true
    });
    $('.ui-loader').find('span').removeClass('ui-icon-loading').addClass('ui-custom-errorIcon');

    setTimeout(function () {
        $.mobile.loading('hide');
        $('div[data-role="content"]').unwrap();
        $('.ui-loader').find('span').removeClass('ui-custom-errorIcon').addClass('ui-icon-loading');
    }, 1000);
}

var popupVideoHeight = 390;
var popupVideoWidth = 640;
function initVideoPopup() {
    function scale(width, height, padding, border) {
        var scrWidth = $(window).width() - 30,
            scrHeight = $(window).height() - 30,
            ifrPadding = 2 * padding,
            ifrBorder = 2 * border,
            ifrWidth = width + ifrPadding + ifrBorder,
            ifrHeight = height + ifrPadding + ifrBorder,
            h, w;

        if (ifrWidth < scrWidth && ifrHeight < scrHeight) {
            w = ifrWidth;
            h = ifrHeight;
        } else if ((ifrWidth / scrWidth) > (ifrHeight / scrHeight)) {
            w = scrWidth;
            h = (scrWidth / ifrWidth) * ifrHeight;
        } else {
            h = scrHeight;
            w = (scrHeight / ifrHeight) * ifrWidth;
        }

        return {
            'width': w - (ifrPadding + ifrBorder),
            'height': h - (ifrPadding + ifrBorder)
        };
    };
    $("#popupVideo").find("a").click(function () {
        $("#popupVideo").popup("close");
        $("#popupVideo").popup("close"); //Sometimes twice??
    });

    $("#popupVideo iframe")
       .attr("width", 0)
       .attr("height", 0);

    $("#popupVideo").on({
        popupbeforeposition: function () {
            var size = scale(popupVideoWidth, popupVideoHeight, 15, 1),
                w = size.width,
                h = size.height;

            $("#popupVideo iframe")
                .attr("width", w)
                .attr("height", h);
        },
        popupafterclose: function () {
            $("#popupVideo iframe")
                .attr("width", 0)
                .attr("height", 0)
                .attr("src", "");
        }
    });
};

function showYoutube(artist, title) {
    request({
        url: "http://gdata.youtube.com/feeds/api/videos/-/Music?max-results=1&alt=json&format=5&q=" + artist + " " + title,
        dataType: "json",
    }, "external")
    .done(function (result) {
        if (result && result.feed && result.feed.entry && result.feed.entry.length > 0) {
            var vidId = result.feed.entry[0].id.$t.replace("http://gdata.youtube.com/feeds/api/videos/", "");
            popupVideoHeight = 390;
            popupVideoWidth = 640;

            $("#popupVideo iframe").attr("src", "http://www.youtube.com/embed/" + vidId + "?&autoplay=1&rel=0&showinfo=0&disablekb=1&autohide=1");
            $("#popupVideo").popup("open");
        }
    });
}

function showSpotify(artist, title) {
    request({
        url: "http://ws.spotify.com/search/1/track.json?q=" + title + "+artist:" + artist,
        dataType: "json",
    }, "external")
    .done(function (result) {
        if (result && result.tracks && result.tracks.length > 0) {
            var spotId = result.tracks[0].href;
            popupVideoHeight = 80;
            popupVideoWidth = 300;

            $("#popupVideo iframe").attr("src", "https://embed.spotify.com/?uri=" + spotId);
            $("#popupVideo").popup("open");
        }
    });
}

function showWikipedia(artist) {
    popupVideoHeight = 800;
    popupVideoWidth = 600;

    $("#popupVideo iframe").attr("src", "http://m.wikipedia.org/wiki/Special:Search/" + artist);
    $("#popupVideo").popup("open");
}

function initTranslation() {
    $.i18n.init({
        resGetPath: 'locales/__lng__.json',
        supportedLngs: ['en', 'de'],
        fallbackLng: 'en',
        keyseparator: '::',
        nsseparator: ':::'
    }).done(function () {
        tranlationLoaded.resolve();
    });
}

function translate() {
    //repair broken buttons on the first page (get broken while translating)
    var repairButtons = function () {
        $($('#discoverConnect')[0].childNodes).wrap('<span class="ui-btn-inner"><span class="ui-btn-text"> </span></span>');
        $($('#discoverReadQr')[0].childNodes).wrap('<span class="ui-btn-inner"><span class="ui-btn-text"> </span></span>');
    };
    
    $('body').i18n();
    repairButtons();
}