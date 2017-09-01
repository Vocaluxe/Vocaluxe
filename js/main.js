//Swipebox
//Use .swipebox class
(function($) {

  $(".swipebox").swipebox();

})(jQuery);

//Download Cards

function showCard(card) {
  card.addClass("animated flipInY").removeClass("flipOutY hidden");
}

function hideCard(card) {
  card.addClass("flipOutY").removeClass("flipInY");
}

// Nightlies
$(".dark-bg .btnDownload").click(function() {
  showCard($(".dark-bg .card-overlay"));
});

$(".dark-bg .card-overlay .close-icon").click(function () {
  hideCard($(".dark-bg .card-overlay"));
});

// Pre Release
$(".red-bg .btnDownload").click(function() {
  showCard($(".red-bg .card-overlay"));
});

$(".red-bg .card-overlay .close-icon").click(function () {
  hideCard($(".red-bg .card-overlay"));
});

// Stable
$(".blue-bg .btnDownload").click(function() {
  showCard($(".blue-bg .card-overlay"));
});

$(".blue-bg .card-overlay .close-icon").click(function () {
  hideCard($(".blue-bg .card-overlay"));
});
