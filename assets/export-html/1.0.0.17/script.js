$(document).ready(function() {
	$("a[href='#sort']").click(function(e) {
		e.preventDefault();
		var key = $(this).attr("data-sort");
		console.log(key);

		$("#list").find("article").sort(function(a, b) {
			if(a.dataset[key] < b.dataset[key]) {
				return -1
			} else {
				return (a.dataset[key] > b.dataset[key]);
			}
		}).appendTo("#list")
	});
	$(".ad").click(function(e) {
		$(".note").remove();
		$(this).parent().parent().append('<div class="note">If your browser displays the content of the file instead of downloading it, press <strong>[Ctrl]+[S]</strong> to download the file.</div>');
	});
});