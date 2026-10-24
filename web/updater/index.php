<?php
	if (
		!((PHP_VERSION_ID >= 80235 && PHP_VERSION_ID < 80300) ||
		  (PHP_VERSION_ID >= 80336 && PHP_VERSION_ID < 80400) ||
		  (PHP_VERSION_ID >= 80427 && PHP_VERSION_ID < 80500) ||
		  (PHP_VERSION_ID >= 80512))
	) {
		http_response_code(500);
		die('HLstatsX:CE requires a patched PHP version (PHP 8.2.35+, 8.3.36+, 8.4.27+, or 8.5.12+). Current version: ' . PHP_VERSION);
	}
	$url = str_replace(array("/updater", '/index.php'), array('',''), isset($_SERVER['PHP_SELF']) ? $_SERVER['PHP_SELF'] : getenv('PHP_SELF'));
	header("Location: $url/hlstats.php?mode=updater");
	exit;
?>