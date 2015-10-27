$(document).ready(function() {
	$.fn.tooltipster('setDefaults', {
		theme: '.tooltipster-noir'
	});
	$(".DownloadArrow").tooltipster({
		position: 'left',
	}).tooltipster("content","Direct Download");
	$(".InfoTitle").tooltipster();
	$(".InfoTitle[data-function='artist']").tooltipster("content","Find more songs from this artist.");
	$(".InfoTitle[data-function='creator']").tooltipster("content","Find more songs from this beatmapper.");
	$(".InfoTitle[data-function='discussion']").tooltipster("content","Opens the beatmap overview and scrolls to the discussion.");
	$(".InfoTitle[data-function='overview']").tooltipster("content","Opens the beatmap overview.");
	$("[title]").tooltipster();
	$("a[href^='#Sort_']").click(function(e) {
		e.preventDefault();
		if ($(this).attr("href") == "#Sort_Artist") {
			$("#ListWrapper").find("article").sort(function(a, b) {
				if (a.dataset.artist < b.dataset.artist) {
					return -1
				} else if (a.dataset.artist > b.dataset.artist) {
					return 1
				} else {
					return 0
				}
			}).appendTo("#ListWrapper")
		} else if ($(this).attr("href") == "#Sort_Creator") {
			$("#ListWrapper").find("article").sort(function(a, b) {
				if (a.dataset.creator < b.dataset.creator) {
					return -1
				} else if (a.dataset.creator > b.dataset.creator) {
					return 1
				} else {
					return 0
				}
			}).appendTo("#ListWrapper")
		} else if ($(this).attr("href") == "#Sort_SetName") {
			$("#ListWrapper").find("article").sort(function(a, b) {
				if (a.dataset.setname < b.dataset.setname) {
					return -1
				} else if (a.dataset.setname > b.dataset.setname) {
					return 1
				} else {
					return 0
				}
			}).appendTo("#ListWrapper")
		} else if ($(this).attr("href") == "#Sort_SetID") {
			$("#ListWrapper").find("article").sort(function(a, b) {
				if (a.dataset.setid < b.dataset.setid) {
					return -1
				} else if (a.dataset.setid > b.dataset.setid) {
					return 1
				} else {
					return 0
				}
			}).appendTo("#ListWrapper")
		}
	})
});