var ownProfileId = 1;
var profileIdRequest = 1;

$(document).ready(function () {
    replaceTransitionHandler();
    initPageLoadHandler();
    initKeyboardPageHandler();
    initMainPageHandler();
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
    $(document).on('pagebeforeshow', '#displayProfile', function () {
        var promise = $.ajax({
            url: "getProfile?profileId=" + profileIdRequest
        }).done(function (result) {
            $('#playerName').attr("value", result.PlayerName);
            if (result.Avatar && result.Avatar.base64Data) {
                $('#playerAvatar').attr("src", result.Avatar.base64Data);
            }
            $('#playerType').attr("value", result.Type);
            $('#playerDifficulty').attr("value", result.Difficulty);
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
                                $('#playerAvatar').attr("src", e.target.result);
                                $('#playerAvatar').data("changed", true);
                                $('#captureContainer').remove();
                            };
                            reader.readAsDataURL(file);
                        }
                    });

                    $('#capture').click();
                });
            }
            else {
                $('#playerName').prop('disabled', true);
                $('#playerType').prop('disabled', true);
                $('#playerDifficulty').prop('disabled', true);
                $('#playerAvatar').unbind("click");
            }
        });

        // Save promise on page so the transition handler can find it.
        $(this).data('promise', promise);
    });

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

}

function initKeyboardPageHandler() {
    $('#keyboardButtonUp').click(function () {
        $.ajax({
            url: "sendKeyEvent=up"
        })
    });

    $('#keyboardButtonDown').click(function () {
        $.ajax({
            url: "sendKeyEvent=down"
        })
    });

    $('#keyboardButtonLeft').click(function () {
        $.ajax({
            url: "sendKeyEvent=left"
        })
    });

    $('#keyboardButtonRight').click(function () {
        $.ajax({
            url: "sendKeyEvent=right"
        })
    });

    $('#keyboardButtonEscape').click(function () {
        $.ajax({
            url: "sendKeyEvent=escape"
        })
    });

    $('#keyboardButtonkeyboardButtonTab').click(function () {
        $.ajax({
            url: "sendKeyEvent=tab"
        })
    });

    $('#keyboardButtonReturn').click(function () {
        $.ajax({
            url: "sendKeyEvent=return"
        })
    });

    $('#keyboardButtonKeys').keyup(function (e) {
        var c = String.fromCharCode(e.keyCode);
        if (c.match(/\w/)) {
            c = e.keyCode >= 65 ? c.toLowerCase() : c;
            $.ajax({
                url: "sendKeyEvent=" + c
            })
        }
        var oldText = $('#keyboardButtonKeys')[0].value;
        if (oldText.length > 0) {
            $('#keyboardButtonKeys')[0].value = oldText.slice(1);
        }
    });
}

function initMainPageHandler() {
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